use std::{alloc, cell::RefCell};

#[cfg(not(target_endian = "little"))]
compile_error!("big-endian targets are not supported");

mod transpose;
mod rasterize;
mod voxels;
mod derasterize;

use rasterize::Rasterizer;
use derasterize::Derasterizer;

#[cfg_attr(test, derive(Debug, Eq, PartialEq))]
#[derive(Copy, Clone, Default, bytemuck::Pod, bytemuck::Zeroable)]
#[repr(C, packed)]
pub struct Octnode {
    pub mat: u8,
    pub dist: u8,
    pub child: u16,
}

pub use voxels::{VoxelOp, AddSphere, AddPyramid};

pub fn octree_voxel_op<A: alloc::Allocator>(
    octree: &[Octnode],
    center: [f32; 3], op: impl VoxelOp,
    alloc: A,
) -> Box<[Octnode], A> {
    thread_local! {
        static SCRATCH: RefCell<(Rasterizer, Derasterizer)> = const {
            RefCell::new((Rasterizer::new(), Derasterizer::new()))
        };
    }

    SCRATCH.with_borrow_mut(|(r, d)| {
        let voxels = r.rasterize(octree);
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
        rasterize::Rasterizer,
        voxels::{Voxel, VoxelGrid, voxel_op},
        derasterize::Derasterizer,
    };
}
