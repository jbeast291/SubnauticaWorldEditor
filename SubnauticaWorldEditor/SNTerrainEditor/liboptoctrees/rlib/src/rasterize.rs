use sn_terrain::{Octree, Octnode, VoxelGrid, Voxel};

#[derive(Copy, Clone)]
pub struct Rasterizer {
    grid: VoxelGrid,
}

impl Rasterizer {
    #[inline(always)]
    pub const fn new() -> Self {
        Rasterizer { grid: bytemuck::zeroed() }
    }

    pub fn rasterize(&mut self, octree: &Octree) -> &mut VoxelGrid {
        self.grid.0.rasterize_node(octree, octree.root());
        &mut self.grid
    }
}

trait RasterizeNode {
    fn rasterize_node(&mut self, octree: &Octree, node: &Octnode);
}

macro_rules! impl_rasterize_node {
    ($vol:literal => $next:literal $(=> $subseq:literal)*) => {
        impl RasterizeNode for [Voxel; $vol] {
            #[inline(always)]
            fn rasterize_node(&mut self, octree: &Octree, node: &Octnode) {
                match octree.node_children(node) {
                    Some(children) => {
                        let voxels: &mut [[Voxel; $next]; 8] = bytemuck::must_cast_mut(self);
                        for (voxels, child) in voxels.iter_mut().zip(children) {
                            voxels.rasterize_node(octree, child);
                        }
                    },

                    None => {
                        let node: u32 = bytemuck::must_cast(*node);
                        self.fill(bytemuck::must_cast(node as u16));
                    },
                }
            }
        }

        impl_rasterize_node!($next $(=> $subseq)*);
    };

    ($done:literal) => { };
}
impl_rasterize_node!(32768 => 4096 => 512 => 64 => 8);

impl RasterizeNode for [Voxel; 8] {
    #[inline(always)]
    fn rasterize_node(&mut self, octree: &Octree, node: &Octnode) {
        *self = match octree.node_children(node) {
            Some(children) => {
                let children: [u32; 8] = bytemuck::must_cast(*children);
                bytemuck::must_cast(children.map(|c| c as u16))
            },

            None => {
                let node: u32 = bytemuck::must_cast(*node);
                bytemuck::must_cast([node as u16; 8])
            },
        }
    }
}

#[cfg(test)]
mod test {
    use super::*;

    #[test]
    fn rasterize_empty_grid() {
        let octree = Octree::EMPTY;

        let mut r = Rasterizer::new();
        let grid = r.rasterize(octree);

        let expect = const { &VoxelGrid([Voxel { mat: 0, dist: 0 }; _]) };
        assert_eq!(&*grid, expect);
    }

    #[test]
    fn rasterize_sphere_grid() {
        let octree = Octree::new(bytemuck::cast_slice({
            include_bytes!("../cases/sphere-octree.bin")
        }));

        let mut r = Rasterizer::new();
        let grid = r.rasterize(octree);

        let expect = const {
            &VoxelGrid(bytemuck::must_cast(*include_bytes!("../cases/sphere-grid.bin")))
        };
        assert_eq!(&*grid, expect);
    }

    #[test]
    fn rasterize_sphere_then_empty() {
        let sphere_octree = Octree::new(bytemuck::cast_slice({
            include_bytes!("../cases/sphere-octree.bin")
        }));
        let empty_octree = Octree::EMPTY;

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
