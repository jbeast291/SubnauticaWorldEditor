use std::array;
use ggmath::Vec3;
use sn_terrain::{VoxelGrid, Voxel};
use wide::{u16x8, i16x8, i32x8, f32x8};

// TODO: aabb optimizations

pub trait VoxelOp {
    fn compile(self) -> impl DistanceFunction;
}

pub trait DistanceFunction: Sync {
    fn evaluate(&self, at: Vec3<f32>) -> u8;
    fn evaluate_simd(&self, at: Vec3<f32x8>) -> u16x8 {
        u16x8::new(array::from_fn(|i| self.evaluate(at.map(|n| n.as_array()[i])) as u16))
    }
}

#[derive(Clone, Copy)]
#[repr(C)]
pub struct AddSphere {
    pub radius: f32,
}

struct AddSphereFunc {
    sq_radius: f32x8,
    rsq_radius: f32x8,
}

impl VoxelOp for AddSphere {
    fn compile(self) -> impl DistanceFunction {
        let sq_radius = self.radius * self.radius;
        let rsq_radius = sq_radius.recip();
        let sq_radius = f32x8::splat(sq_radius);
        let rsq_radius = f32x8::splat(rsq_radius);

        AddSphereFunc { sq_radius, rsq_radius }
    }
}

impl DistanceFunction for AddSphereFunc {
    fn evaluate(&self, at: Vec3<f32>) -> u8 {
        let sq_dist = at.length_squared();
        if sq_dist >= self.sq_radius.as_array()[0] { 0 } else {
            ((1.0 - sq_dist * self.rsq_radius.as_array()[0]) * 252.0).round_ties_even() as u8
        }
    }

    fn evaluate_simd(&self, at: Vec3<f32x8>) -> u16x8 {
        let sq_dist = at.length_squared();

        let dist = (f32x8::ONE - sq_dist * self.rsq_radius) * f32x8::splat(252.0);
        let dist = sq_dist.simd_ge(self.sq_radius).select(f32x8::ZERO, dist);
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
    #[expect(unused)] height: f32,
    #[expect(unused)] base_halfwidth: f32,
}

impl VoxelOp for AddPyramid {
    fn compile(self) -> impl DistanceFunction {
        let AddPyramid { height, base_halfwidth } = self;
        AddPyramidFunc { height, base_halfwidth }
    }
}

impl DistanceFunction for AddPyramidFunc {
    fn evaluate(&self, #[expect(unused)] at: Vec3<f32>) -> u8 {
        todo!("pyramid sdf")
    }
}

// TODO: move `center` into `op`

pub fn voxel_op(
    voxels: &mut VoxelGrid,
    center: [f32; 3],
    op: impl VoxelOp,
) {
    let voxels = bytemuck::cast_slice_mut::<Voxel, u16x8>(&mut voxels.0);
    let center = Vec3::new(center[0], center[1], center[2]).map(|n| f32x8::splat(n));
    let op = op.compile();

    voxels.iter_mut().enumerate().for_each(|(i, voxel)| {
        let at = center - voxel_grid_coords_simd(i).map(|n| f32x8::from_i32x8(n));
        let dist = op.evaluate_simd(at) & 0xFF;

        *voxel = dist.simd_gt(0).select((dist << 8) | u16x8::splat(4), *voxel);
    })
}

fn voxel_grid_coords_simd(idx: usize) -> Vec3<i32x8> {
    let idx = idx * 8;
    let [x, y, z] = VoxelGrid::coords(idx);

    Vec3::new(
        i32x8::splat(x) + i32x8::new([0, 0, 0, 0, 1, 1, 1, 1]),
        i32x8::splat(y) + i32x8::new([0, 0, 1, 1, 0, 0, 1, 1]),
        i32x8::splat(z) + i32x8::new([0, 1, 0, 1, 0, 1, 0, 1]),
    )
}

// TODO: add tests

// Selection Enum (sphere, AAcuboid, capsule/spline)
// Operation Enum (fill, clear, paint, flatten, smooth)

// blockPos + [+0.0, 1.0]
