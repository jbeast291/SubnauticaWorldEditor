use std::alloc;
use crate::{voxel_op::{Voxel, VoxelGrid}, Octnode, Depth, MaxDepth};

pub fn rasterize<A: alloc::Allocator>(octree: &[Octnode], alloc: A) -> Box<VoxelGrid, A> {
    fn recurse(
        octree: &[Octnode],
        voxels: &mut [Voxel; 32 * 32 * 32],
        grid_offset: usize,
        node_index: usize,
        depth: impl Depth,
    ) {
        let layer = 5 - depth.remaining();
        depth.descend(
            voxels,
            |voxels, depth| {
                let node = octree[node_index];
                let child_index = node.child as usize;

                if child_index == 0 {
                    let voxel = Voxel { mat: node.mat, dist: node.dist };
                    for i in 0..get_volume_for_layer(layer) {
                        voxels[grid_offset + i] = voxel;
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
                voxels[grid_offset] = Voxel { mat: node.mat, dist: node.dist };
            },
        );
    }

    let mut voxels = alloc_zeroed_box_in::<VoxelGrid, A>(alloc);
    if !octree.is_empty() {
        recurse(octree, &mut voxels.0, 0, 0, MaxDepth::<5>);
    }
    voxels
}

const fn get_volume_for_layer(layer: usize) -> usize {
    1 << (15 - (3 * layer as usize))
}

fn alloc_zeroed_box_in<T: bytemuck::Zeroable, A: alloc::Allocator>(alloc: A) -> Box<T, A> {
    let layout = alloc::Layout::new::<T>();
    let Ok(ptr) = alloc::Allocator::allocate_zeroed(&alloc, layout) else {
        alloc::handle_alloc_error(layout)
    };

    let ptr = ptr.as_ptr().cast::<T>();

    // SAFETY: `T` is valid when zero-initialized, and `ptr` was successfully allocated by `alloc`
    // with the correct size and alignment.
    unsafe { Box::from_raw_in(ptr, alloc) }
}

#[cfg(test)]
mod test {
    use super::*;

    #[test]
    fn rasterize_empty_grid() {
        let octree = &[Octnode { mat: 0, dist: 0, child: 0 }];
        let grid = rasterize(octree, alloc::Global);

        let expect = const { &VoxelGrid([Voxel { mat: 0, dist: 0 }; _]) };
        assert_eq!(&*grid, expect);
    }

    #[test]
    fn rasterize_sphere_grid() {
        let octree = bytemuck::cast_slice(include_bytes!("../cases/sphere-octree.bin"));
        let grid = rasterize(octree, alloc::Global);

        let expect = const {
            &VoxelGrid(bytemuck::must_cast(*include_bytes!("../cases/sphere-grid.bin")))
        };
        assert_eq!(&*grid, expect);
    }
}
