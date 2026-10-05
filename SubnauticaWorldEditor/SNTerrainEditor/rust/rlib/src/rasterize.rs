use std::mem::MaybeUninit;
use crate::{Octnode, Voxel, Depth, MaxDepth};

#[repr(C, align(16))]
pub struct VoxelGridOutput {
    pub array: [MaybeUninit<Voxel>; 32 * 32 * 32],
}

pub fn rasterize(octree: &[Octnode], voxels: &mut VoxelGridOutput) {
    fn recurse(
        octree: &[Octnode],
        voxels: &mut [MaybeUninit<Voxel>; 32 * 32 * 32],
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

    if octree.is_empty() { return }
    recurse(octree, &mut voxels.array, 0, 0, MaxDepth::<5>);
}

const fn get_volume_for_layer(layer: usize) -> usize {
    1 << (15 - (3 * layer as usize))
}

#[cfg(test)]
mod test {
    use crate::VoxelGrid;
    use super::*;

    #[test]
    fn rasterize_empty_grid() {
        let octree = &[Octnode { mat: 0, dist: 0, child: 0 }];
        let mut grid = VoxelGridOutput {
            array: [const { MaybeUninit::new(Voxel { mat: 0, dist: 0 }) }; _],
        };

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
        let mut grid = VoxelGridOutput {
            array: [const { MaybeUninit::new(Voxel { mat: 0, dist: 0 }) }; _],
        };

        rasterize(octree, &mut grid);

        // SAFETY: `rasterize` never unitializes any of the values in `grid`.
        let grid = unsafe {
            &*((&mut grid as *mut VoxelGridOutput).cast_const().cast::<VoxelGrid>())
        };

        let expect = const {
            &VoxelGrid { array: bytemuck::must_cast(*include_bytes!("../cases/sphere-grid.bin")) }
        };
        assert_eq!(grid, expect);
    }
}
