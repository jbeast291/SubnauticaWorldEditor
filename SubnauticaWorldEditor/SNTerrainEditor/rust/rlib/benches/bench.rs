use std::{hint::black_box, mem::MaybeUninit, time::Duration};
use criterion::{Criterion, criterion_main, criterion_group};
use rand::{prelude::*, rngs};
use optoctrees::{Octnode, Voxel, VoxelGrid, VoxelGridOutput, VoxelOperation};

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
    targets = rasterize, voxel_op, derasterize
}

fn rasterize(c: &mut Criterion) {
    let mut g = c.benchmark_group("rasterize");

    bench(&mut g, "empty_octree", &[Octnode { mat: 0, dist: 0, child: 0 }]);

    bench(&mut g, "sphere_octree", {
        bytemuck::cast_slice(include_bytes!("../cases/sphere-octnodes.bin"))
    });

    fn bench(g: &mut Group, name: &str, voxels: &[Octnode]) {
        let mut grid = VoxelGridOutput {
            array: [const { MaybeUninit::new(Voxel { mat: 0, dist: 0 }) }; _],
        };
        g.bench_function(name, |b| b.iter(|| {
            black_box(optoctrees::rasterize(black_box(voxels), black_box(&mut grid)))
        }));
    }
}

fn voxel_op(c: &mut Criterion) {
    let mut g = c.benchmark_group("voxel_op");

    bench(&mut g, "add_sphere", optoctrees::AddSphere { scale: 5.75 });

    fn bench(g: &mut Group, name: &str, op: impl VoxelOperation + Copy) {
        let mut grid = bytemuck::zeroed();
        g.bench_function(name, |b| b.iter(|| {
            black_box(optoctrees::voxel_op(
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
        &VoxelGrid { array: bytemuck::must_cast(*include_bytes!("../cases/sphere-grid.bin")) }
    });

    let mut random_grid = bytemuck::zeroed::<VoxelGrid>();
    rngs::Xoshiro256PlusPlus::seed_from_u64(204).fill({
        bytemuck::must_cast_slice_mut::<Voxel, u16>(&mut random_grid.array)
    });
    bench(&mut g, "random_grid", &random_grid);

    fn bench(g: &mut Group, name: &str, voxels: &VoxelGrid) {
        g.bench_function(name, |b| b.iter(|| {
            let mut buf = optoctrees::ReverseOctreeBuffer::new();
            let nodes = optoctrees::derasterize(&mut buf, black_box(voxels));
            let nodes = alloc_octree(nodes);
            black_box(nodes)
        }));

        fn alloc_octree(nodes: &[Octnode]) -> Box<[Octnode]> {
            use std::{alloc, ptr};

            let len = nodes.len();
            if len == 0 { Box::new([]) } else {
                let layout = alloc::Layout::array::<u32>(len)
                    .expect("octnode array len should be in bounds");

                // SAFETY: `layout` does not have a size of zero. The allocated pointer is not null
                // if we use it. `dst` is fully initialized before it becomes a `Box`.
                unsafe {
                    let dst = alloc::alloc(layout).cast::<Octnode>();
                    if dst.is_null() { alloc::handle_alloc_error(layout) }
                    ptr::copy_nonoverlapping(nodes.as_ptr(), dst, len);
                    Box::from_raw(ptr::slice_from_raw_parts_mut(dst, len))
                }
            }
        }
    }
}
