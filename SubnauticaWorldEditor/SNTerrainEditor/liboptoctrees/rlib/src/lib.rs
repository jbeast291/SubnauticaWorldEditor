use std::{alloc, cell::RefCell};
use sn_terrain::{Octree, VoxelGrid};

#[cfg(not(target_endian = "little"))]
compile_error!("big-endian targets are not supported");

mod voxels;
mod derasterize;

use derasterize::Derasterizer;

pub use sn_terrain;

pub use voxels::{VoxelOp, AddSphere, AddPyramid};

pub fn octree_voxel_op<A: alloc::Allocator>(
    octree: &Octree,
    center: [f32; 3], op: impl VoxelOp,
    alloc: A,
) -> Box<Octree, A> {
    thread_local! {
        static SCRATCH: RefCell<(VoxelGrid, Derasterizer)> = const {
            RefCell::new((VoxelGrid::EMPTY, Derasterizer::new()))
        };
    }

    SCRATCH.with_borrow_mut(|(voxels, d)| {
        sn_terrain::rasterize(octree, voxels);
        voxels::voxel_op(voxels, center, op);
        d.derasterize(voxels, alloc)
    })
}

#[expect(nonstandard_style)]
type u8xN = cfg_select! {
    cpu = "x86-64-v4" => wide::u8x64,
    cpu = "x86-64-v3" => wide::u8x32,
    _ => wide::u8x16,
};

#[expect(nonstandard_style)]
type u16xN = cfg_select! {
    cpu = "x86-64-v4" => wide::u16x32,
    cpu = "x86-64-v3" => wide::u16x16,
    _ => wide::u16x8,
};

const NATIVE_SIMD_WIDTH: usize = cfg_select! {
    cpu = "x86-64-v4" => 64,
    cpu = "x86-64-v3" => 32,
    _  => 16,
};

#[cfg(feature = "__bench")]
#[doc(hidden)]
pub mod __bench {
    pub use crate::{
        voxels::voxel_op,
        derasterize::Derasterizer,
    };
}
