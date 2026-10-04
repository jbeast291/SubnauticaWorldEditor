use std::{hint::black_box, mem::MaybeUninit, time::Duration};
use criterion::{criterion_main, criterion_group, Criterion};
use rand::{prelude::*, rngs};
use optoctrees::{Octnode, Voxel, VoxelGrid, VoxelGridOutput};

criterion_main!(benches);
criterion_group! {
    name = benches;
    config = Criterion::default()
        .warm_up_time(Duration::from_secs(5))
        .measurement_time(Duration::from_secs(10))
        .sample_size(1000)
        .significance_level(0.01);
    targets = rasterize, derasterize
}

fn rasterize(c: &mut Criterion) {
    bench(c, "rasterize_empty_octree", &[Octnode { mat: 0, dist: 0, child: 0 }]);

    bench(c, "rasterize_sphere_octree", bytemuck::cast_slice(include_bytes!("../cases/sphere-octnodes.bin")));

    fn bench(c: &mut Criterion, name: &str, voxels: &[Octnode]) {
        let mut grid = VoxelGridOutput { array: [const { MaybeUninit::new(Voxel { mat: 0, dist: 0 }) }; _] };
        c.bench_function(name, |b| b.iter(|| {
            black_box(optoctrees::rasterize(black_box(voxels), black_box(&mut grid)))
        }));
    }
}

fn derasterize(c: &mut Criterion) {
    bench(c, "derasterize_empty_grid", &bytemuck::zeroed());

    bench(c, "derasterize_sphere_grid", const {
        &VoxelGrid { array: bytemuck::must_cast(*include_bytes!("../cases/sphere-grid.bin")) }
    });

    let mut random_grid = bytemuck::zeroed::<VoxelGrid>();
    rngs::Xoshiro256PlusPlus::seed_from_u64(204).fill({
        bytemuck::must_cast_slice_mut::<Voxel, u16>(&mut random_grid.array)
    });
    bench(c, "derasterize_random_grid", &random_grid);

    fn bench(c: &mut Criterion, name: &str, voxels: &VoxelGrid) {
        c.bench_function(name, |b| b.iter(|| {
            let mut buf = optoctrees::ReverseOctreeBuffer::new();
            let nodes = optoctrees::derasterize(&mut buf, black_box(voxels));
            let nodes = alloc_node_array(nodes);
            black_box(nodes)
        }));

        fn alloc_node_array(nodes: &[Octnode]) -> Box<[Octnode]> {
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