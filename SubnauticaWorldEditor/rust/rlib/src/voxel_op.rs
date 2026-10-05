use std::array;
use ggmath::Vec3;
use wide::{u8x16, u16x16, i16x16, i32x16, f32x16};
use crate::{Voxel, VoxelGrid, i32x16_to_u8x16};

pub trait VoxelOperation {
    fn compile(self) -> impl DistanceFunction;
}

pub trait DistanceFunction: Sync {
    fn evaluate(&self, center: Vec3<i16>, block: Vec3<u8>) -> u8;

    fn evaluate_simd(&self, center: Vec3<i16>, block: Vec3<u8x16>) -> u8x16 {
        u8x16::new(array::from_fn(|i| self.evaluate(center, Vec3::new(
            block.x.as_array()[i],
            block.y.as_array()[i],
            block.z.as_array()[i],
        ))))
    }
}

#[derive(Clone, Copy)]
#[repr(C)]
pub struct AddSphere {
    pub scale: f32,
}

struct AddSphereFunc {
    sq_scale: f32,
    rsq_scale: f32,
}

impl VoxelOperation for AddSphere {
    fn compile(self) -> impl DistanceFunction {
        let sq_scale = self.scale * self.scale;
        let rsq_scale = sq_scale.recip();
        AddSphereFunc { sq_scale, rsq_scale }
    }
}

impl DistanceFunction for AddSphereFunc {
    fn evaluate(&self, center: Vec3<i16>, block: Vec3<u8>) -> u8 {
        let delta = (block.map(|n| n as i16) - center).map(|n| n as f32);
        let sq_dist = delta.length_squared();

        if sq_dist >= self.sq_scale { 0 } else {
            ((1.0 - sq_dist * self.rsq_scale) * 252.0).round_ties_even() as u8
        }
    }

    fn evaluate_simd(&self, center: Vec3<i16>, block: Vec3<u8x16>) -> u8x16 {
        let delta = block.map(|n| i16x16::from(n)) - center.map(|n| i16x16::splat(n));
        let delta = delta.map(|n| f32x16::from_i32x16(i32x16::from_i16x16(n)));
        let sq_dist = delta.length_squared();

        let dist = (f32x16::ONE - sq_dist * self.rsq_scale) * f32x16::splat(252.0);
        let dist = sq_dist.simd_ge(self.sq_scale).select(f32x16::ZERO, dist);
        let dist = dist.round_ties_even().fast_trunc_int();

        i32x16_to_u8x16(dist)
    }
}

#[derive(Clone, Copy)]
#[repr(C)]
pub struct AddPyramid {
    pub height: f32,
    pub base_halfwidth: f32,
}

struct AddPyramidFunc {
    height: f32,
    base_halfwidth: f32,
}

impl VoxelOperation for AddPyramid {
    fn compile(self) -> impl DistanceFunction {
        let AddPyramid { height, base_halfwidth } = self;
        AddPyramidFunc { height, base_halfwidth }
    }
}

impl DistanceFunction for AddPyramidFunc {
    fn evaluate(&self, center: Vec3<i16>, block: Vec3<u8>) -> u8 {
        todo!()
    }
}

pub fn voxel_op(
    voxels: &mut VoxelGrid,
    center: [i16; 3],
    op: impl VoxelOperation,
) {
    let voxels = bytemuck::cast_slice_mut::<Voxel, u16x16>(&mut voxels.array);
    let center = Vec3::new(center[0], center[1], center[2]);
    let op = op.compile();

    voxels.iter_mut().enumerate().for_each(|(i, voxel)| {
        let block = voxel_grid_coords_simd(i);
        let dist: u16x16 = op.evaluate_simd(center, block).into();

        *voxel = dist.simd_gt(0).select((dist << 8) | u16x16::splat(4), *voxel);
    })
}

// FIXME: make this simd
fn voxel_grid_coords_simd(idx: usize) -> Vec3<u8x16> {
    let idx = idx * 16;

    let arr = array::from_fn(|i| voxel_grid_coords(idx + i));
    Vec3::new(
        u8x16::new(arr.map(|v| v.x as u8)),
        u8x16::new(arr.map(|v| v.y as u8)),
        u8x16::new(arr.map(|v| v.z as u8)),
    )
}

fn voxel_grid_coords(idx: usize) -> Vec3<isize> {
    let idx = idx as isize;
    Vec3::new(
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
