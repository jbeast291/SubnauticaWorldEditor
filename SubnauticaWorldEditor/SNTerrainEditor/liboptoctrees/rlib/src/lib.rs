use std::alloc;

#[cfg(not(target_endian = "little"))]
compile_error!("big-endian targets are not supported");

mod transpose;
mod rasterize;
mod voxel_op;
mod derasterize;

#[cfg_attr(test, derive(Debug, Eq, PartialEq))]
#[derive(Copy, Clone, Default, bytemuck::Pod, bytemuck::Zeroable)]
#[repr(C, packed)]
pub struct Octnode {
    pub mat: u8,
    pub dist: u8,
    pub child: u16,
}

pub use voxel_op::{VoxelOp, AddSphere, AddPyramid};

pub fn octree_voxel_op<A: alloc::Allocator>(
    octree: &[Octnode],
    center: [f32; 3], op: impl VoxelOp,
    alloc: A,
) -> Box<[Octnode], A> {
    let mut voxels = rasterize::rasterize(octree, alloc::Global);
    voxel_op::voxel_op(&mut voxels, center, op);
    derasterize::derasterize(&voxels, alloc)
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

#[cfg(feature = "__bench")]
#[doc(hidden)]
pub mod __bench {
    pub use crate::{
        rasterize::rasterize,
        voxel_op::{Voxel, VoxelGrid, voxel_op},
        derasterize::derasterize,
    };
}
