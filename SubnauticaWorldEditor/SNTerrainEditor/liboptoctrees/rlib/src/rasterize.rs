use crate::{voxels::{Voxel, VoxelGrid}, Octnode, Depth, MaxDepth, AtDepth};

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
        if let Some(root) = octree.first() {
            rasterize_recurse(&mut self.grid, 0, octree, root, MaxDepth::<5>);
        } else {
            self.grid = bytemuck::zeroed();
        }

        &mut self.grid
    }
}

#[inline(always)]
fn rasterize_recurse(
    voxels: &mut VoxelGrid, vox_idx: usize,
    octree: &[Octnode], node: &Octnode,
    depth: impl RasterDepth,
) {
    let volume = depth.volume();
    depth.descend(
        voxels,
        #[inline(always)] |voxels, depth| {
            if let Some(children) = node_children(octree, node) {
                let volume = depth.volume();
                let mut vox_idx = vox_idx;
                for child in children {
                    rasterize_recurse(voxels, vox_idx, octree, child, depth);
                    vox_idx += volume;
                }
            } else {
                let voxel = Voxel { mat: node.mat, dist: node.dist };
                voxels.0[vox_idx..vox_idx + volume].fill(voxel);
            }
        },
        #[inline(always)] |voxels| {
            voxels.0[vox_idx] = Voxel { mat: node.mat, dist: node.dist };
        },
    );
}

#[inline(always)]
fn node_children<'o>(octree: &'o [Octnode], node: &'o Octnode) -> Option<&'o [Octnode; 8]> {
    if node.child == 0 { None } else { octree.get(node.child as usize..)?.first_chunk() }
}

trait RasterDepth: Depth<Descend = Self::RasterDescend> {
    type RasterDescend: RasterDepth;
    fn volume(&self) -> usize;
}

macro_rules! impl_raster_depth {
    ($($num:literal),* $(,)?) => {$(
        impl RasterDepth for MaxDepth<$num> {
            type RasterDescend = Self::Descend;

            #[inline(always)]
            fn volume(&self) -> usize { const { (1usize << $num).pow(3) } }
        }
    )*};
}
impl_raster_depth!(5, 4, 3, 2, 1, 0);

impl RasterDepth for AtDepth {
    type RasterDescend = Self::Descend;

    #[inline(always)]
    fn volume(&self) -> usize { match *self { } }
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
