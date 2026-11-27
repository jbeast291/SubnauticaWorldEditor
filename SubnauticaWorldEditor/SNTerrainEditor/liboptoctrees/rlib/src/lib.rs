use std::alloc;
use sn_terrain::Octree;

#[cfg(not(target_endian = "little"))]
compile_error!("big-endian targets are not supported");

mod voxels;

pub use sn_terrain;

pub use voxels::{VoxelOp, AddSphere, AddPyramid};

pub fn octree_voxel_op<A: alloc::Allocator>(
    octree: &Octree,
    center: [f32; 3], op: impl VoxelOp,
    alloc: A,
) -> Box<Octree, A> {
    let mut voxels = octree.rasterize();
    voxels::voxel_op(&mut voxels, center, op);
    let octree = voxels.derasterize::<Box<Octree>>();
    octree.alloc_cloned(alloc)
}

#[cfg(feature = "__bench")]
#[doc(hidden)]
pub mod __bench {
    pub use crate::voxels::voxel_op;
}
