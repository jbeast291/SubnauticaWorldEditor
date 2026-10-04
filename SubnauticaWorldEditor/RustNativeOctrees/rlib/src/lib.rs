use std::{array, mem::{self, MaybeUninit}, slice};
use wide::{u16x8, u32x8};

#[cfg(not(target_endian = "little"))]
compile_error!("big-endian targets are not supported");

#[cfg(all(
    any(target_arch = "x86", target_arch = "x86_64"),
    not(any(doc, target_feature = "sse4.2")),
))]
compile_error!("sse4.2 is required on x86 targets for performance");

#[derive(Debug, Eq, PartialEq)]
#[derive(Copy, Clone, Default, bytemuck::Pod, bytemuck::Zeroable)]
#[repr(C, align(2))]
pub struct Voxel {
    pub mat: u8,
    pub dist: u8,
}

#[cfg_attr(test, derive(Debug, Eq, PartialEq))]
#[derive(Copy, Clone, bytemuck::Pod, bytemuck::Zeroable)]
#[repr(C, align(16))]
pub struct VoxelGrid {
    pub array: [Voxel; 32 * 32 * 32],
}

#[repr(C, align(16))]
pub struct VoxelGridOutput {
    pub array: [MaybeUninit<Voxel>; 32 * 32 * 32],
}

#[cfg_attr(test, derive(Debug, Eq, PartialEq))]
#[derive(Copy, Clone, Default, bytemuck::Pod, bytemuck::Zeroable)]
#[repr(C, packed)]
pub struct Octnode {
    pub mat: u8,
    pub dist: u8,
    pub child: u16,
}

pub fn rasterize(octree: &[Octnode], voxels: &mut VoxelGridOutput) {
    fn recurse(
        octree: &[Octnode], 
        voxels: &mut [MaybeUninit<Voxel>; 32 * 32 * 32],
        grid_offset: usize,
        node_index: usize,
        depth: impl Depth,
    ) {
        let layer= 5 - depth.remaining();
        depth.descend(
            voxels,
            |voxels, depth| {
                let node = octree[node_index];
                let child_index = node.child as usize;

                if child_index == 0 {
                    let voxel = Voxel { mat: node.mat, dist: node.dist };
                    for i in 0..get_volume_for_layer(layer) {
                        voxels[grid_offset + i].write(voxel);
                    }
                    return
                }

                let child_volume = get_volume_for_layer(layer + 1);

                for i in 0..8 {
                    recurse(
                        octree,
                        voxels,
                        grid_offset + child_volume * i,
                        child_index + i,
                        depth,
                    );
                }
            },
            |voxels| { 
                let node = octree[node_index];
                voxels[grid_offset].write(Voxel { mat: node.mat, dist: node.dist });
            },
        );
    }

    const fn get_volume_for_layer(layer: usize) -> usize {
        1 << (15 - (3 * layer as usize))
    }

    if octree.is_empty() { return }

    recurse(octree, &mut voxels.array, 0, 0, MaxDepth::<5>);
}

#[inline]
pub fn derasterize<'b>(buf: &'b mut ReverseOctreeBuffer, voxels: &VoxelGrid) -> &'b mut [Octnode] {
    buf.clear();
    let voxels = bytemuck::cast_slice::<Voxel, u16x8>(&voxels.array);

    fn recurse(
        buf: &mut ReverseOctreeBuffer, voxels: &[u16x8],
        idx: &mut usize, depth: impl Depth,
    ) -> u32 {
        let children = depth.descend(
            idx,
            |idx, depth| {
                let mut children = array::from_fn(|_| recurse(buf, voxels, idx, depth));
                children.reverse();
                u32x8::from(children)
            },
            |idx| {
                *idx -= 1;
                u32x8::from(voxels[*idx])
            },
        );

        let first = children.to_array()[0];

        let grandchildren = !(children & 0xFFFF_00_00).simd_eq(0).all();
        let identical = children.simd_eq(first).all();
        let empty = (children & 0x0000_00_FF).simd_eq(0);
        let solid = (children & 0x0000_FF_00).simd_eq(0).all() && (empty.all() || !empty.any());

        if !grandchildren && (identical || solid) { first } else {
            let mat = parent_material(children);
            let mut dist = parent_distance(children);
            if dist == 0 && mat != 0 { dist = 1; }

            let child = buf.push_chunk(children) as u32;
            child << 16 | dist << 8 | mat
        }
    }

    fn parent_material(children: u32x8) -> u32 {
        let mats = children & 0x0000_00_FF;
        let nonsolid = (children & 0x0000_FF_00).min(u32x8::splat(1));

        let mut surface_counts = u32x8::splat(0);
        let mut volume_counts = u32x8::splat(0);
        for i in 0..8 {
            let hits = mats.simd_eq(mats.to_array()[i]) & 1;
            surface_counts += hits & nonsolid & nonsolid.to_array()[i];
            volume_counts += hits;
        }

        const REV_INDICES: u32x8 = u32x8::new([7, 6, 5, 4, 3, 2, 1, 0]);
        let nonempty = mats.min(u32x8::splat(1));

        let keys: u32x8 = nonempty << 19 |
            surface_counts << 15 | volume_counts << 11 |
            REV_INDICES << 8 | mats;

        keys.reduce_max() & 0xFF
    }

    fn parent_distance(children: u32x8) -> u32 {
        let dists: u32x8 = (children & 0x0000_FF_00) >> 8;
        let mats = children & 0x0000_00_FF;

        let sum = (dists.simd_eq(0) & mats.simd_ne(0))
            .select(u32x8::splat(252), dists)
            .reduce_add();

        (sum + 3 + ((sum >> 3) & 1)) >> 3
    }

    let root = recurse(buf, voxels, &mut voxels.len(), MaxDepth::<4>);
    bytemuck::must_cast_slice_mut(buf.finish(root))
}

const MAX_OCTNODES: u16 = 32 * 32 * 32 + 16 * 16 * 16 + 8 * 8 * 8 + 4 * 4 * 4 + 2 * 2 * 2 + 1;

pub struct ReverseOctreeBuffer {
    // INVARIANT: All elements of `buf` from `start` onwards must be initialized. `start` must not
    // be greater than the length of `buf`.
    buf: [MaybeUninit<u32x8>; MAX_OCTNODES.div_ceil(8) as usize],
    start: usize,
}

impl ReverseOctreeBuffer {
    #[inline]
    pub fn new() -> Self {
        let buf = [MaybeUninit::uninit(); _];
        let start = buf.len();
        ReverseOctreeBuffer { buf, start }
    }

    fn clear(&mut self) { self.start = self.buf.len() }

    fn push_chunk(&mut self, chunk: u32x8) -> usize {
        let Some(idx) = self.start.checked_sub(1) else {
            panic!("octree buffer capacity overflow")
        };

        // SAFETY: `idx` is one less than `start`, which is no greater than the length of `buf` per
        // invariants.
        unsafe { self.buf.get_unchecked_mut(idx).write(chunk); }

        self.start = idx;
        idx * 8
    }

    fn finish(&mut self, mut root: u32) -> &mut [u32] {
        let Some(root_idx) = (self.start * 8).checked_sub(1) else {
            panic!("octree buffer capacity overflow")
        };

        {
            let child = root >> 16;
            if child != 0 {
                let voxel = root & 0x0000_FF_FF;
                let child = child.wrapping_sub(root_idx as u32) & 0xFFFF;
                root = (child << 16) | voxel;
            }
        }

        for chunk in self.chunks_mut() {
            let child: u32x8 = *chunk >> 16;
            let voxel = *chunk & 0x0000_FF_FF;

            let offset = (child - root_idx as u32) & 0xFFFF;
            let child = child.simd_eq(0).select(child, offset);

            *chunk = (child << 16) | voxel;
        }

        self.clear();
        let nodes = self.buf_nodes_mut();

        // SAFETY: `nodes` has a length eight times that of `buf`. `root_idx` is one node less than
        // `start * 8` and is in-bounds, and `start` is no greater than the length of `buf` per
        // invariants.
        unsafe { nodes.get_unchecked_mut(root_idx).write(root); }

        // SAFETY: `root_idx` is in bounds for `nodes` per above. All nodes from `root_idx` onwards
        // are initialized; the root node was initialized above, while the others must have been
        // initialized per invariants.
        unsafe { nodes.get_unchecked_mut(root_idx..).assume_init_mut() }
    }

    fn chunks_mut(&mut self) -> &mut [u32x8] {
        // SAFETY: `start` is no greater than the length of `buf` per invariants.
        let slice = unsafe { self.buf.get_unchecked_mut(self.start..) };

        // SAFETY: All elements of `buf` from `start` onwards are initialized per invariants.
        unsafe { slice.assume_init_mut() }
    }

    fn buf_nodes_mut(&mut self) -> &mut [MaybeUninit<u32>] {
        const {
            use mem::{size_of as size, align_of as align};
            if size::<u32x8>() != size::<u32>() * 8 || align::<u32x8>() % align::<u32>() != 0 {
                panic!("invalid octnode chunk simd layout")
            }
        }

        let ptr = self.buf.as_mut_ptr().cast();
        let len = self.buf.len() * 8;

        // SAFETY: `ptr` is a pointer to `buf`, and `len` is the length of `buf`. `u32x8` is eight
        // times the size of `u32` and has a greater or equal alignment.
        unsafe { slice::from_raw_parts_mut(ptr, len) }
    }
}

trait Depth: Copy {
    type Descend: Depth;
    fn descend<T, R>(
        self, val: T,
        descend: impl FnOnce(T, Self::Descend) -> R,
        done: impl FnOnce(T) -> R,
    ) -> R;
    fn remaining(&self) -> usize;
}

#[derive(Copy, Clone)]
struct MaxDepth<const N: usize>;

macro_rules! impl_depth {
    ($($num:literal),* $(,)?) => {$(
        impl Depth for MaxDepth<$num> {
            type Descend = MaxDepth<{ $num - 1 }>;
            fn descend<T, R>(
                self, val: T,
                descend: impl FnOnce(T, Self::Descend) -> R,
                _: impl FnOnce(T) -> R,
            ) -> R {
                descend(val, MaxDepth)
            }
            fn remaining(&self) -> usize { $num }
        }
    )*};
}
impl_depth!(5, 4, 3, 2, 1);

impl Depth for MaxDepth<0> {
    type Descend = AtDepth;
    fn descend<T, R>(
        self, val: T,
        _: impl FnOnce(T, AtDepth) -> R,
        done: impl FnOnce(T) -> R,
    ) -> R {
        done(val)
    }
    fn remaining(&self) -> usize { 0 }
}

#[derive(Copy, Clone)]
enum AtDepth { }

impl Depth for AtDepth {
    type Descend = AtDepth;
    fn descend<T, R>(
        self, _: T,
        _: impl FnOnce(T, AtDepth) -> R,
        _: impl FnOnce(T) -> R,
    ) -> R {
        match self { }
    }
    fn remaining(&self) -> usize { match *self { } }
}

#[cfg(test)]
mod test {
    use super::*;

    #[test]
    fn derasterize_empty_grid() {
        let mut buf = ReverseOctreeBuffer::new();
        let grid = VoxelGrid { array: [Voxel { mat: 0, dist: 0 }; _] };
        let nodes = derasterize(&mut buf, &grid);

        let expect = &[Octnode { mat: 0, dist: 0, child: 0 }];
        assert_eq!(nodes, expect);
    }

    #[test]
    fn derasterize_sphere_grid() {
        let mut buf = ReverseOctreeBuffer::new();
        let grid = const {
            &VoxelGrid { array: bytemuck::must_cast(*include_bytes!("../cases/sphere-grid.bin")) }
        };
        let nodes = derasterize(&mut buf, &grid);

        let expect = bytemuck::cast_slice(include_bytes!("../cases/sphere-octnodes.bin"));
        assert_eq!(nodes, expect);
    }

    #[test]
    fn rasterize_empty_grid() {
        let octree = &[Octnode { mat: 0, dist: 0, child: 0 }];
        let mut grid = VoxelGridOutput { array: [const { MaybeUninit::new(Voxel { mat: 0, dist: 0 }) }; _] };
  
        rasterize(octree, &mut grid);

        // SAFETY: `rasterize` never unitializes any of the values in `grid`.
        let grid = unsafe {
            &*((&mut grid as *mut VoxelGridOutput).cast_const().cast::<VoxelGrid>())
        };

        let expect = const { &VoxelGrid { array: [Voxel { mat: 0, dist: 0 }; _] } };
        assert_eq!(grid, expect);
    }

    #[test]
    fn rasterize_sphere_grid() {
        let octree = bytemuck::cast_slice(include_bytes!("../cases/sphere-octnodes.bin"));
        let mut grid = VoxelGridOutput { array: [const { MaybeUninit::new(Voxel { mat: 0, dist: 0 }) }; _] };

        rasterize(octree, &mut grid);

        // SAFETY: `rasterize` never unitializes any of the values in `grid`.
        let grid = unsafe {
            &*((&mut grid as *mut VoxelGridOutput).cast_const().cast::<VoxelGrid>())
        };

        let expect = const { &VoxelGrid { array: bytemuck::must_cast(*include_bytes!("../cases/sphere-grid.bin")) } };
        assert_eq!(grid, expect);
    }
}
