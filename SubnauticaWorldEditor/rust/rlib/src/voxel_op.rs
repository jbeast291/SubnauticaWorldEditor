use std::array;
use glam::{ISizeVec3, Vec3};
use wide::{u8x16, u16x16};
use crate::{Voxel, VoxelGrid};

pub trait VoxelOperation {
    type Func: DistanceFunction;
    fn compile(self) -> Self::Func;
}

pub trait DistanceFunction: Sync {
    fn evaluate(&self, center: ISizeVec3, block: ISizeVec3) -> u8;

    fn evaluate_simd(&self, center: ISizeVec3, block: [u8x16; 3]) -> u8x16 {
        let block = block.map(|item| item.to_array());
        u8x16::new(array::from_fn(|i| {
            let block = ISizeVec3::new(
                block[0][i] as isize,
                block[1][i] as isize,
                block[2][i] as isize,
            );
            self.evaluate(center, block)
        }))
    }
}

#[derive(Clone, Copy)]
#[repr(C)]
pub struct AddSphere {
    pub scale: f32,
}

pub struct AddSphereFunc {
    sq_scale: f32,
    rsq_scale: f32,
}

impl VoxelOperation for AddSphere {
    type Func = AddSphereFunc;
    fn compile(self) -> AddSphereFunc {
        let sq_scale = self.scale * self.scale;
        let rsq_scale = sq_scale.recip();
        AddSphereFunc { sq_scale, rsq_scale }
    }
}

impl DistanceFunction for AddSphereFunc {
    fn evaluate(&self, center: ISizeVec3, block: ISizeVec3) -> u8 {
        let delta: Vec3 = (block - center).as_vec3();
        let sq_dist = delta.length_squared();

        if sq_dist >= self.sq_scale {
            0
        } else {
            ((1.0 - (sq_dist * self.rsq_scale)) * 252.0).round() as u8
        }
    }
}

#[derive(Clone, Copy)]
#[repr(C)]
pub struct AddPyramid {
    pub height: f32,
    pub base_halfwidth: f32,
}

pub struct AddPyramidFunc {
    height: f32,
    base_halfwidth: f32,
}

impl VoxelOperation for AddPyramid {
    type Func = AddPyramidFunc;
    fn compile(self) -> AddPyramidFunc {
        let AddPyramid { height, base_halfwidth } = self;
        AddPyramidFunc { height, base_halfwidth }
    }
}

impl DistanceFunction for AddPyramidFunc {
    fn evaluate(&self, center: ISizeVec3, block: ISizeVec3) -> u8 {
        todo!()
    }
}

pub fn voxel_op(
    voxels: &mut VoxelGrid,
    center: [isize; 3],
    op: impl VoxelOperation,
) {
    let voxels = bytemuck::cast_slice_mut::<Voxel, u16x16>(&mut voxels.array);
    let center = ISizeVec3 { x: center[0], y: center[1], z: center[2] };
    let op = op.compile();

    voxels.iter_mut().enumerate().for_each(|(i, voxel)| {
        let block = voxel_grid_coords_simd(i);
        let dist: u16x16 = op.evaluate_simd(center, block).into();

        *voxel = dist.simd_gt(0).select((dist << 8) | u16x16::splat(4), *voxel);
    })
}

// FIXME: make this simd
fn voxel_grid_coords_simd(idx: usize) -> [u8x16; 3] {
    let idx = idx * 16;

    let arr = array::from_fn(|i| voxel_grid_coords(idx + i));
    [
        u8x16::new(arr.map(|v| v.x as u8)),
        u8x16::new(arr.map(|v| v.y as u8)),
        u8x16::new(arr.map(|v| v.z as u8)),
    ]
}

fn voxel_grid_coords(idx: usize) -> ISizeVec3 {
    let idx = idx as isize;
    ISizeVec3::new(
        (idx >> 2 & 0b00001) |
        (idx >> 4 & 0b00010) |
        (idx >> 6 & 0b00100) |
        (idx >> 8 & 0b01000) |
        (idx >> 10 & 0b10000),
        (idx >> 1 & 0b00001) |
        (idx >> 3 & 0b00010) |
        (idx >> 5 & 0b00100) |
        (idx >> 7 & 0b01000) |
        (idx >> 9 & 0b10000),
        (idx >> 0 & 0b00001) |
        (idx >> 2 & 0b00010) |
        (idx >> 4 & 0b00100) |
        (idx >> 6 & 0b01000) |
        (idx >> 8 & 0b10000),
    )
}

/*
fn voxel_grid_index([x, y, z]: [usize; 3]) -> usize {
    // FIXME: use `deposit_bits` when it's stabilized
    /*
    z.deposit_bits(0b001_001_001_001_001) |
    y.deposit_bits(0b010_010_010_010_010) |
    x.deposit_bits(0b100_100_100_100_100)
    */
    (z & 0b00001) << 0 |
    (z & 0b00010) << 2 |
    (z & 0b00100) << 4 |
    (z & 0b01000) << 6 |
    (z & 0b10000) << 8 |

    (y & 0b00001) << 1 |
    (y & 0b00010) << 3 |
    (y & 0b00100) << 5 |
    (y & 0b01000) << 7 |
    (y & 0b10000) << 9 |

    (x & 0b00001) << 2 |
    (x & 0b00010) << 4 |
    (x & 0b00100) << 6 |
    (x & 0b01000) << 8 |
    (x & 0b10000) << 10
}
*/
