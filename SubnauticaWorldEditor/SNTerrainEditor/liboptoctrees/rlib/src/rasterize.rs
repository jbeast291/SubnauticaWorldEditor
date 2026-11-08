use crate::{voxel_op::{Voxel, VoxelGrid}, Octnode, Depth, MaxDepth};

#[derive(Copy, Clone)]
pub struct Rasterizer {
    grid: VoxelGrid,
}

impl Rasterizer {
    #[inline(always)]
    pub const fn new() -> Self {
        Rasterizer { grid: bytemuck::zeroed() }
    }

    pub fn rasterize(&mut self, octree: &[Octnode]) -> &mut VoxelGrid {
        if octree.is_empty() {
            self.grid = bytemuck::zeroed();
        } else {
            rasterize_recurse(octree, &mut self.grid.0, 0, 0, MaxDepth::<5>);
        }

        &mut self.grid
    }
}

fn rasterize_recurse(
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
                rasterize_recurse(
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

const fn get_volume_for_layer(layer: usize) -> usize {
    1 << (15 - (3 * layer as usize))
}

#[cfg(test)]
mod test {
    use super::*;

    #[test]
    fn rasterize_empty_grid() {
        let octree = &[Octnode { mat: 0, dist: 0, child: 0 }];

        let mut r = Rasterizer::new();
        let grid = r.rasterize(octree);

        let expect = const { &VoxelGrid([Voxel { mat: 0, dist: 0 }; _]) };
        assert_eq!(&*grid, expect);
    }

    #[test]
    fn rasterize_sphere_grid() {
        let octree = bytemuck::cast_slice(include_bytes!("../cases/sphere-octree.bin"));

        let mut r = Rasterizer::new();
        let grid = r.rasterize(octree);

        let expect = const {
            &VoxelGrid(bytemuck::must_cast(*include_bytes!("../cases/sphere-grid.bin")))
        };
        assert_eq!(&*grid, expect);
    }

    #[test]
    fn rasterize_sphere_then_empty() {
        let sphere_octree = bytemuck::cast_slice(include_bytes!("../cases/sphere-octree.bin"));
        let empty_octree = &[Octnode { mat: 0, dist: 0, child: 0 }];

        let mut r = Rasterizer::new();

        let sphere_grid = r.rasterize(sphere_octree);
        let expect_sphere = const {
            &VoxelGrid(bytemuck::must_cast(*include_bytes!("../cases/sphere-grid.bin")))
        };
        assert_eq!(&*sphere_grid, expect_sphere);

        let empty_grid = r.rasterize(empty_octree);
        let expect_empty = const { &VoxelGrid([Voxel { mat: 0, dist: 0 }; _]) };
        assert_eq!(&*empty_grid, expect_empty);
    }
}
