#[cfg(not(target_endian = "little"))]
compile_error!("big-endian targets are not supported");

#[cfg(all(
    any(target_arch = "x86", target_arch = "x86_64"),
    not(any(doc, target_feature = "sse4.2")),
))]
compile_error!("sse4.2 is required on x86 targets for performance");

mod rasterize;
mod voxel_op;
mod derasterize;

#[cfg_attr(test, derive(Debug, Eq, PartialEq))]
#[derive(Copy, Clone, Default, bytemuck::Pod, bytemuck::Zeroable)]
#[repr(C, align(2))]
pub struct Voxel {
    pub mat: u8,
    pub dist: u8,
}

#[cfg_attr(test, derive(Debug, Eq, PartialEq))]
#[derive(Copy, Clone, bytemuck::Pod, bytemuck::Zeroable)]
#[repr(C, align(32))]
pub struct VoxelGrid {
    pub array: [Voxel; 32 * 32 * 32],
}

#[cfg_attr(test, derive(Debug, Eq, PartialEq))]
#[derive(Copy, Clone, Default, bytemuck::Pod, bytemuck::Zeroable)]
#[repr(C, packed)]
pub struct Octnode {
    pub mat: u8,
    pub dist: u8,
    pub child: u16,
}

pub use rasterize::{rasterize, VoxelGridOutput};
pub use voxel_op::{voxel_op, VoxelOperation, AddSphere, AddPyramid};
pub use derasterize::{derasterize, ReverseOctreeBuffer};

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
