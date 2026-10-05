use std::array;
use ggmath::Vec3;
use wide::{u16x8, i16x8, i32x8, f32x8};
use crate::{Voxel, VoxelGrid};

pub trait VoxelOperation {
    fn compile(self) -> impl DistanceFunction;
}

pub trait DistanceFunction: Sync {
    fn evaluate(&self, at: Vec3<i16>) -> u8;
    fn evaluate_simd(&self, at: Vec3<i16x8>) -> u16x8 {
        u16x8::new(array::from_fn(|i| self.evaluate(at.map(|n| n.as_array()[i])) as u16))
    }
}

#[derive(Clone, Copy)]
#[repr(C)]
pub struct AddSphere {
    pub scale: f32,
}

struct AddSphereFunc {
    sq_scale: f32x8,
    rsq_scale: f32x8,
}

impl VoxelOperation for AddSphere {
    fn compile(self) -> impl DistanceFunction {
        let sq_scale = self.scale * self.scale;
        let rsq_scale = sq_scale.recip();
        let sq_scale = f32x8::splat(sq_scale);
        let rsq_scale = f32x8::splat(rsq_scale);

        AddSphereFunc { sq_scale, rsq_scale }
    }
}

impl DistanceFunction for AddSphereFunc {
    fn evaluate(&self, at: Vec3<i16>) -> u8 {
        let sq_dist = at.map(|n| n as f32).length_squared();
        if sq_dist >= self.sq_scale.as_array()[0] { 0 } else {
            ((1.0 - sq_dist * self.rsq_scale.as_array()[0]) * 252.0).round_ties_even() as u8
        }
    }

    fn evaluate_simd(&self, at: Vec3<i16x8>) -> u16x8 {
        let sq_dist = at.map(|n| f32x8::from_i32x8(i32x8::from_i16x8(n))).length_squared();

        let dist = (f32x8::ONE - sq_dist * self.rsq_scale) * f32x8::splat(252.0);
        let dist = sq_dist.simd_ge(self.sq_scale).select(f32x8::ZERO, dist);
        let dist = dist.round_ties_even().fast_trunc_int();

        i16x8::from_i32x8_truncate(dist).cast_unsigned()
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
    fn evaluate(&self, at: Vec3<i16>) -> u8 {
        todo!("pyramid sdf")
    }
}

pub fn voxel_op(
    voxels: &mut VoxelGrid,
    center: [i16; 3],
    op: impl VoxelOperation,
) {
    let voxels = bytemuck::cast_slice_mut::<Voxel, u16x8>(&mut voxels.array);
    let center = Vec3::new(center[0], center[1], center[2]).map(|n| i16x8::splat(n));
    let op = op.compile();

    voxels.iter_mut().enumerate().for_each(|(i, voxel)| {
        let at = voxel_grid_coords_simd(i) - center;
        let dist = op.evaluate_simd(at);

        *voxel = dist.simd_gt(0).select((dist << 8) | u16x8::splat(4), *voxel);
    })
}

fn voxel_grid_coords_simd(idx: usize) -> Vec3<i16x8> {
    // FIXME: make this simd and do less math overall

    let idx = idx * 8;
    let arr = array::from_fn(|i| voxel_grid_coords(idx + i));
    Vec3::new(
        i16x8::new(arr.map(|v| v.x as i16)),
        i16x8::new(arr.map(|v| v.y as i16)),
        i16x8::new(arr.map(|v| v.z as i16)),
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

// TODO: add tests
