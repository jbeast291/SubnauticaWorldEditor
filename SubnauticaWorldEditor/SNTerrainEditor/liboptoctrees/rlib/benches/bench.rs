use std::{alloc, hint::black_box, time::Duration};
use criterion::{Criterion, criterion_main, criterion_group};
use rand::{prelude::*, rngs};
use sn_terrain::{Octree, VoxelGrid, Voxel};
use optoctrees::VoxelOp;

type Group<'c> = criterion::BenchmarkGroup<'c, criterion::measurement::WallTime>;

criterion_main!(benches);
criterion_group! {
    name = benches;
    config = Criterion::default()
        .warm_up_time(Duration::from_secs(5))
        .measurement_time(Duration::from_secs(10))
        .sample_size(1000)
        .noise_threshold(0.05)
        .significance_level(0.01);
    targets = octree_voxel_op, rasterize, voxel_op, derasterize
}

fn octree_voxel_op(c: &mut Criterion) {
    let mut g = c.benchmark_group("octree_voxel_op");

    bench(&mut g, "add_sphere", optoctrees::AddSphere { radius: 5.75 });

    fn bench(g: &mut Group, name: &str, op: impl VoxelOp + Copy) {
        let octree = Octree::EMPTY;
        g.bench_function(name, |b| b.iter(|| {
            black_box(optoctrees::octree_voxel_op(
                black_box(octree),
                black_box([16.0, 16.0, 16.0]),
                black_box(op),
                black_box(alloc::Global),
            ))
        }));
    }
}

fn rasterize(c: &mut Criterion) {
    let mut g = c.benchmark_group("rasterize");

    bench(&mut g, "empty_octree", Octree::EMPTY);

    bench(&mut g, "sphere_octree", Octree::new(bytemuck::cast_slice({
        include_bytes!("../cases/sphere-octree.bin")
    })));

    fn bench(g: &mut Group, name: &str, octree: &Octree) {
        g.bench_function(name, |b| b.iter(|| {
            black_box(black_box(octree).rasterize())
        }));
    }
}

fn voxel_op(c: &mut Criterion) {
    let mut g = c.benchmark_group("voxel_op");

    bench(&mut g, "add_sphere", optoctrees::AddSphere { radius: 5.75 });

    fn bench(g: &mut Group, name: &str, op: impl VoxelOp + Copy) {
        let mut grid = bytemuck::zeroed();
        g.bench_function(name, |b| b.iter(|| {
            black_box(optoctrees::__bench::voxel_op(
                black_box(&mut grid),
                black_box([16.0, 16.0, 16.0]),
                black_box(op),
            ))
        }));
    }
}

fn derasterize(c: &mut Criterion) {
    let mut g = c.benchmark_group("derasterize");

    bench(&mut g, "empty_grid", &bytemuck::zeroed());

    bench(&mut g, "sphere_grid", const {
        &VoxelGrid(bytemuck::must_cast(*include_bytes!("../cases/sphere-grid.bin")))
    });

    let mut random_grid = bytemuck::zeroed::<VoxelGrid>();
    rngs::Xoshiro256PlusPlus::seed_from_u64(204).fill({
        bytemuck::must_cast_slice_mut::<Voxel, u16>(&mut random_grid.0)
    });
    bench(&mut g, "random_grid", &random_grid);

    fn bench(g: &mut Group, name: &str, voxels: &VoxelGrid) {
        g.bench_function(name, |b| b.iter(|| {
            black_box(black_box(voxels).derasterize::<Box<Octree>>())
        }));
    }
}
