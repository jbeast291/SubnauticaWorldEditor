use std::{alloc, array, iter, mem::MaybeUninit, num::NonZero, ptr};
use wide::{u8x16, u16x8, u16x16, u32x8};
use crate::{transpose, voxels::VoxelGrid, Octnode, u8xN, u16xN, NATIVE_SIMD_WIDTH};

#[derive(Copy, Clone)]
pub struct Derasterizer {
    layers: OctreeLayers,
}

impl Derasterizer {
    #[inline(always)]
    pub const fn new() -> Self {
        Derasterizer {
            layers: OctreeLayers {
                level1: [LayerChunk::ZERO; _],
                level2: [LayerChunk::ZERO; _],
                level3: [LayerChunk::ZERO; _],
                level4: [LayerChunk::ZERO; _],
                level5: [LayerChunk::ZERO; _],
            },
        }
    }

    pub fn derasterize<A: alloc::Allocator>(
        &mut self, voxels: &VoxelGrid, alloc: A,
    ) -> Box<[Octnode], A> {
        let voxels: &[u16xN; LEVEL0_CHUNKS * 2] = bytemuck::must_cast_ref(voxels);
        let nodes = calc_layers(&mut self.layers, voxels);

        let voxels: &[u16x8; 4096] = bytemuck::must_cast_ref(voxels);
        emit_octree(voxels, &self.layers, nodes, alloc)
    }
}

const LEVEL0_CHUNKS: usize = 32768usize.div_ceil(NATIVE_SIMD_WIDTH);
const LEVEL1_CHUNKS: usize = 4096usize.div_ceil(NATIVE_SIMD_WIDTH);
const LEVEL2_CHUNKS: usize = 512usize.div_ceil(NATIVE_SIMD_WIDTH);
const LEVEL3_CHUNKS: usize = 64usize.div_ceil(NATIVE_SIMD_WIDTH);
const LEVEL4_CHUNKS: usize = 8usize.div_ceil(NATIVE_SIMD_WIDTH);
const LEVEL5_CHUNKS: usize = 1usize.div_ceil(NATIVE_SIMD_WIDTH);

#[derive(Copy, Clone)]
struct OctreeLayers {
    level1: [LayerChunk; LEVEL1_CHUNKS],
    level2: [LayerChunk; LEVEL2_CHUNKS],
    level3: [LayerChunk; LEVEL3_CHUNKS],
    level4: [LayerChunk; LEVEL4_CHUNKS],
    level5: [LayerChunk; LEVEL5_CHUNKS],
}

#[derive(Copy, Clone)]
struct LayerChunk {
    mat: u8xN,
    dist: u8xN,
    is_parent: u8xN,
}

impl LayerChunk {
    const ZERO: Self = LayerChunk { mat: u8xN::ZERO, dist: u8xN::ZERO, is_parent: u8xN::ZERO };
}

fn calc_layers(layers: &mut OctreeLayers, voxels: &[u16xN; LEVEL0_CHUNKS * 2]) -> NonZero<usize> {
    let mut nodes = NonZero::<usize>::MIN;
    let mut count_chunk = |chunk: &LayerChunk| {
        nodes = nodes.checked_add(chunk.is_parent.to_bitmask().count_ones() as usize * 8)
            .expect("total nodes should be in range for usize");
    };

    calc_first_layer(&mut layers.level1, voxels, &mut count_chunk);
    calc_parent_layer(&mut layers.level2, &layers.level1, &mut count_chunk);
    calc_parent_layer(&mut layers.level3, &layers.level2, &mut count_chunk);
    calc_parent_layer(&mut layers.level4, &layers.level3, &mut count_chunk);
    calc_parent_layer(&mut layers.level5, &layers.level4, &mut count_chunk);

    nodes
}

#[inline(always)]
fn calc_first_layer(
    layer: &mut [LayerChunk; LEVEL1_CHUNKS],
    voxels: &[u16xN; LEVEL0_CHUNKS * 2],
    mut count_chunk: impl FnMut(&LayerChunk),
) {
    let voxel_chunks: &[[u16xN; 16]; LEVEL1_CHUNKS] = bytemuck::must_cast_ref(voxels);
    for (chunk_voxels, slot) in voxel_chunks.iter().zip(layer) {
        let first = u16xN::splat(chunk_voxels[0].to_array()[0]);
        let identical = chunk_voxels.iter().fold(u16xN::MAX, |a, b| a & b.simd_eq(first)).all();

        let chunk = if identical {
            let [mat, dist] = first.as_array()[0].to_le_bytes();
            LayerChunk { mat: u8xN::splat(mat), dist: u8xN::splat(dist), is_parent: u8xN::ZERO }
        } else {
            let voxel_bytes: [u8x16; NATIVE_SIMD_WIDTH] = bytemuck::must_cast(*chunk_voxels);
            let interleaved = transpose::transpose_u8_Nx16(voxel_bytes);
            let mat = array::from_fn(|i| interleaved[2 * i]);
            let dist = array::from_fn(|i| interleaved[2 * i + 1]);

            calc_layer_chunk(&mat, &dist, u8xN::ZERO)
        };

        count_chunk(&chunk);
        *slot = chunk;
    }
}

#[inline(always)]
fn calc_parent_layer<const CHUNKS: usize, const PREV: usize>(
    layer: &mut [LayerChunk; CHUNKS],
    prev: &[LayerChunk; PREV],
    mut count_chunk: impl FnMut(&LayerChunk),
) {
    let (chunk_children, rem) = prev.as_chunks::<8>();
    let mut remainder = [LayerChunk::ZERO; 8];
    remainder[..rem.len()].copy_from_slice(rem);
    let chunk_children = chunk_children.iter().chain(iter::once(&remainder));

    for (slot, children) in layer.iter_mut().zip(chunk_children) {
        let mat = transpose::transpose_u8_Nx8(children.map(|chunk| chunk.mat));
        let dist = transpose::transpose_u8_Nx8(children.map(|chunk| chunk.dist));
        let is_parent = transpose::transpose_u8_Nx8(children.map(|chunk| chunk.is_parent));

        let is_grandparent = is_parent.iter().fold(u8xN::ZERO, |a, &b| a | b);

        let chunk = calc_layer_chunk(&mat, &dist, is_grandparent);
        count_chunk(&chunk);
        *slot = chunk;
    }
}

#[inline(always)]
fn calc_layer_chunk(mat: &[u8xN; 8], dist: &[u8xN; 8], is_grandparent: u8xN) -> LayerChunk {
    let empty: [u8xN; 8] = array::from_fn(|i| mat[i].simd_eq(u8xN::ZERO));
    let solid: [u8xN; 8] = array::from_fn(|i| dist[i].simd_eq(u8xN::ZERO));

    let (mut identical, mut all_empty, mut all_solid) = (u8xN::MAX, u8xN::MAX, u8xN::MAX);
    let mut any_empty = u8xN::ZERO;
    for i in 0..8 {
        identical &= mat[i].simd_eq(mat[0]) & dist[i].simd_eq(dist[0]);
        all_empty &= empty[i];
        all_solid &= solid[i];
        any_empty |= empty[i];
    }

    let homogenous_children = identical | (all_solid & (all_empty | !any_empty));
    let is_parent = is_grandparent | !homogenous_children;

    if is_parent.to_bitmask() == 0 {
        return LayerChunk { mat: mat[0], dist: dist[0], is_parent: u8xN::ZERO }
    }

    let parent_mat = calc_parent_mat(mat, &empty, &solid);
    let parent_dist = calc_parent_dist(dist, &empty, &solid);

    let parent_dist = parent_dist.max(parent_mat.simd_ne(u8xN::ZERO) & u8xN::splat(1));

    LayerChunk {
        mat: is_parent.select(parent_mat, mat[0]),
        dist: is_parent.select(parent_dist, dist[0]),
        is_parent,
    }
}

#[inline(always)]
fn calc_parent_mat(mat: &[u8xN; 8], empty: &[u8xN; 8], solid: &[u8xN; 8]) -> u8xN {
    let vote_weight: [u8xN; 8] = array::from_fn(|i| {
        u8xN::splat(0x01) | (!solid[i] & u8xN::splat(0x10))
    });

    let mut best_score = u8xN::ZERO;
    let mut best_mat = mat[0];
    for candidate in 0..8 {
        let mut votes = u8xN::ZERO;
        for voter in 0..8 {
            votes += mat[candidate].simd_eq(mat[voter]) & vote_weight[voter];
        }

        let vote_mask = u8xN::splat(0x0F) | (!solid[candidate] & u8xN::splat(0xF0));
        let score = !empty[candidate] & votes & vote_mask;

        let new_best_score = best_score.max(score);
        best_mat = new_best_score.simd_eq(best_score).select(best_mat, mat[candidate]);
        best_score = new_best_score;
    }

    best_mat
}

#[inline(always)]
fn calc_parent_dist(dist: &[u8xN; 8], empty: &[u8xN; 8], solid: &[u8xN; 8]) -> u8xN {
    cfg_select! {
        cpu = "x86-64-v3" => {
            let mut sum = [u16x16::ZERO; NATIVE_SIMD_WIDTH / 16];
            for i in 0..8 {
                let dist = dist[i] | (solid[i] & !empty[i] & u8xN::splat(252));
                let dist: [u8x16; NATIVE_SIMD_WIDTH / 16] = bytemuck::must_cast(dist);

                for (sum, dist) in sum.iter_mut().zip(dist) {
                    *sum += u16x16::from(dist);
                }
            }

            let mean: [u8x16; NATIVE_SIMD_WIDTH / 16] = sum.map(|sum| {
                let mean: u16x16 = (sum + 3 + ((sum >> 3) & 1u16)) >> 3;
                u8x16::new(mean.to_array().map(|n| n as u8))
            });

            bytemuck::must_cast(mean)
        },

        _ => {
            let mut sum = u16x16::ZERO;
            for i in 0..8 {
                sum += u16x16::from(dist[i] | (solid[i] & !empty[i] & u8x16::splat(252)));
            }

            let mean: u16x16 = (sum + 3 + ((sum >> 3) & 1u16)) >> 3;
            u8x16::new(mean.to_array().map(|n| n as u8))
        },
    }
}

fn emit_octree<A: alloc::Allocator>(
    voxels: &[u16x8; 4096], layers: &OctreeLayers,
    nodes: NonZero<usize>, alloc: A,
) -> Box<[Octnode], A> {
    let nodes = NonZero::<u16>::try_from(nodes)
        .expect("octree buffer len should not overflow u16");
    let mut buf = ReverseOctreeBuffer::alloc(nodes, alloc);

    let root = emit_children(voxels, layers, &mut buf, MaxDepth::<3>, 0);
    buf.finish(root)
}

#[inline(always)]
fn emit_children(
    voxels: &[u16x8; 4096], layers: &OctreeLayers,
    buf: &mut ReverseOctreeBuffer<impl alloc::Allocator>,
    depth: impl EmitDepth, idx: usize,
) -> u32 {
    let layer = depth.layer(layers);
    let (node_chunk, lane) = (&layer[idx / NATIVE_SIMD_WIDTH], idx % NATIVE_SIMD_WIDTH);

    let (mat, dist) = (node_chunk.mat.to_array()[lane], node_chunk.dist.to_array()[lane]);
    let voxel = mat as u32 | (dist as u32) << 8;
    if node_chunk.is_parent.to_array()[lane] == 0 { return voxel }

    let children = depth.descend(
        &mut *buf,
        #[inline(always)] |buf, depth| {
            let mut children = [0; 8];
            for c in (0..8).rev() {
                children[c] = emit_children(voxels, layers, buf, depth, 8 * idx + c);
            }
            u32x8::from(children)
        },
        #[inline(always)] |buf| emit_grandchildren(voxels, &layers.level1, buf, idx),
    );

    let pos = buf.push_siblings(children);
    voxel | (pos as u32) << 16
}

#[inline(always)]
fn emit_grandchildren(
    voxels: &[u16x8; 4096], layer: &[LayerChunk; LEVEL1_CHUNKS],
    buf: &mut ReverseOctreeBuffer<impl alloc::Allocator>,
    idx: usize,
) -> u32x8 {
    const SUBCHUNKS: usize = NATIVE_SIMD_WIDTH / 8;

    let (children_chunk, subchunk) = (&layer[idx / SUBCHUNKS], idx % SUBCHUNKS);
    let load_subchunk = |field: &u8xN| -> u8x16 {
        let field: &[[u8; 8]; SUBCHUNKS] = bytemuck::must_cast_ref(field);
        bytemuck::must_cast([field[subchunk], [0; 8]])
    };

    let mat = load_subchunk(&children_chunk.mat);
    let dist = load_subchunk(&children_chunk.dist);
    let is_parent = load_subchunk(&children_chunk.is_parent);

    let mut children = [0; 8];
    for c in (0..8).rev() {
        if is_parent.to_array()[c] != 0 {
            children[c] = buf.push_siblings(u32x8::from(voxels[8 * idx + c]));
        }
    }
    let children = u16x8::new(children);

    let voxel: u16x8 = bytemuck::must_cast(u8x16::unpack_low(mat, dist));
    u32x8::from(children) << 16 | u32x8::from(voxel)
}

trait EmitDepth: Copy {
    type Descend: EmitDepth;

    fn descend<T, R>(
        self, val: T,
        descend: impl FnOnce(T, Self::Descend) -> R,
        done: impl FnOnce(T) -> R,
    ) -> R;

    fn layer<'l>(&self, layers: &'l OctreeLayers) -> &'l [LayerChunk];
}

#[derive(Copy, Clone)]
struct MaxDepth<const N: usize>;

#[derive(Copy, Clone)]
enum AtDepth { }

macro_rules! impl_emit_depth {
    ($($depth:literal => $layer:ident),* $(,)?) => {$(
        impl EmitDepth for MaxDepth<$depth> {
            type Descend = MaxDepth<{ $depth - 1 }>;

            #[inline(always)]
            fn descend<T, R>(
                self, val: T,
                descend: impl FnOnce(T, Self::Descend) -> R,
                _: impl FnOnce(T) -> R,
            ) -> R {
                descend(val, MaxDepth)
            }

            #[inline(always)]
            fn layer<'l>(&self, layers: &'l OctreeLayers) -> &'l [LayerChunk] {
                &layers.$layer
            }
        }
    )*};
}
impl_emit_depth!(3 => level5, 2 => level4, 1 => level3);

impl EmitDepth for MaxDepth<0> {
    type Descend = AtDepth;

    #[inline(always)]
    fn descend<T, R>(
        self, val: T,
        _: impl FnOnce(T, AtDepth) -> R,
        done: impl FnOnce(T) -> R,
    ) -> R {
        done(val)
    }

    #[inline(always)]
    fn layer<'l>(&self, layers: &'l OctreeLayers) -> &'l [LayerChunk] {
        &layers.level2
    }
}

impl EmitDepth for AtDepth {
    type Descend = AtDepth;

    #[inline(always)]
    fn descend<T, R>(
        self, _: T,
        _: impl FnOnce(T, AtDepth) -> R,
        _: impl FnOnce(T) -> R,
    ) -> R {
        match self { }
    }

    #[inline(always)]
    fn layer<'l>(&self, _: &'l OctreeLayers) -> &'l [LayerChunk] {
        match *self { }
    }
}

struct ReverseOctreeBuffer<A: alloc::Allocator> {
    // INVARIANT: All elements of `buf` from `start` onwards must be initialized. `start` must not
    // be greater than the length of `buf`. `buf` must be at least one node in size. `start` and
    // `buf` must not overflow `u16`.
    buf: Box<[MaybeUninit<Octnode>], A>,
    start: usize,
}

impl<A: alloc::Allocator> ReverseOctreeBuffer<A> {
    fn alloc(len: NonZero<u16>, alloc: A) -> Self {
        let len = len.get() as usize;
        let layout = alloc::Layout::array::<MaybeUninit<Octnode>>(len)
            .expect("octree buffer len should not overflow isize");

        let Ok(ptr) = alloc::Allocator::allocate(&alloc, layout) else {
            alloc::handle_alloc_error(layout)
        };

        let ptr = ptr::slice_from_raw_parts_mut(ptr.as_ptr().cast::<MaybeUninit<Octnode>>(), len);

        // SAFETY: `MaybeUninit<Octnode>` is valid when uninitialized, and `ptr` was successfully
        // allocated by `alloc` with the correct size and alignment.
        let buf = unsafe { Box::from_raw_in(ptr, alloc) };
        ReverseOctreeBuffer { buf, start: len }
    }

    fn push_siblings(&mut self, siblings: u32x8) -> u16 {
        let Some(idx) = self.start.checked_sub(8) else {
            panic!("octree buffer capacity overflow")
        };

        // SAFETY: `idx` is less than `start`, which is no greater than the length of `buf` per
        // invariants.
        unsafe {
            (&raw mut self.buf[0]).add(idx as usize).cast::<u32x8>().write_unaligned(siblings)
        }
        self.start = idx;
        idx as u16
    }

    fn finish(mut self, root: u32) -> Box<[Octnode], A> {
        let Some(root_idx) = self.start.checked_sub(1) else {
            panic!("octree buffer capacity overflow")
        };

        if root_idx != 0 { panic!("octree buffer not filled") }

        // SAFETY: `buf` is always at least one node in size per invariants.
        unsafe { (&raw mut self.buf[0]).cast::<u32>().write_unaligned(root) }

        // SAFETY: All nodes from `start` onwards were initialized per invariants, and the root node
        // was just initialized.
        unsafe { self.buf.assume_init() }
    }
}

#[cfg(test)]
mod test {
    use super::*;

    #[test]
    fn derasterize_empty_grid() {
        let grid = VoxelGrid([crate::voxels::Voxel { mat: 0, dist: 0 }; _]);

        let mut d = Derasterizer::new();
        let nodes = d.derasterize(&grid, alloc::Global);

        let expect = &[Octnode { mat: 0, dist: 0, child: 0 }];
        assert_eq!(&*nodes, expect);
    }

    #[test]
    fn derasterize_sphere_grid() {
        let grid = const {
            &VoxelGrid(bytemuck::must_cast(*include_bytes!("../cases/sphere-grid.bin")))
        };

        let mut d = Derasterizer::new();
        let nodes = d.derasterize(&grid, alloc::Global);

        let expect = bytemuck::cast_slice(include_bytes!("../cases/sphere-octree.bin"));
        assert_eq!(&*nodes, expect);
    }

    #[test]
    fn derasterize_sphere_then_empty() {
        let sphere_grid = const {
            &VoxelGrid(bytemuck::must_cast(*include_bytes!("../cases/sphere-grid.bin")))
        };
        let empty_grid = VoxelGrid([crate::voxels::Voxel { mat: 0, dist: 0 }; _]);

        let mut d = Derasterizer::new();
        let sphere_nodes = d.derasterize(&sphere_grid, alloc::Global);
        let empty_nodes = d.derasterize(&empty_grid, alloc::Global);

        let expect_sphere = bytemuck::cast_slice(include_bytes!("../cases/sphere-octree.bin"));
        let expect_empty = &[Octnode { mat: 0, dist: 0, child: 0 }];
        assert_eq!(&*sphere_nodes, expect_sphere);
        assert_eq!(&*empty_nodes, expect_empty);
    }
}
