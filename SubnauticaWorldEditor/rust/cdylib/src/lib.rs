use std::{ffi, ptr, slice};
use optoctrees::{Octnode, VoxelGrid, VoxelGridOutput};

struct LibOptoctrees {
    alloc_octnode_array: extern "C" fn(usize) -> *mut Octnode,
    #[expect(dead_code)] free_octnode_array: extern "C" fn(*mut Octnode, usize),
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn new_liboptoctrees(
    alloc_octnode_array: extern "C" fn(usize) -> *mut Octnode,
    free_octnode_array: extern "C" fn(*mut Octnode, usize),
) -> *mut ffi::c_void {
    #[cfg(any(target_arch = "x86", target_arch = "x86_64"))]
    if !raw_cpuid::CpuId::new().get_feature_info().map_or(false, |f| f.has_sse42()) {
        panic!("processor does not support the required instructions")
    }

    let lib = LibOptoctrees { alloc_octnode_array, free_octnode_array };
    Box::into_raw(Box::new(lib)).cast()
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn drop_liboptoctrees(lib: *mut ffi::c_void) {
    drop(unsafe { Box::from_raw(lib.cast::<LibOptoctrees>()) });
}

#[repr(C)]
pub struct OctnodeArray { pub ptr: *mut Octnode, pub len: usize }

#[unsafe(no_mangle)]
pub unsafe extern "C" fn optoctree_rasterize(
    _: *const ffi::c_void,
    octree: OctnodeArray,
    voxels: *mut VoxelGridOutput,
) {
    let octree = unsafe { slice::from_raw_parts(octree.ptr, octree.len) };
    let voxels = unsafe { &mut *voxels };

    optoctrees::rasterize(octree, voxels);
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn optoctree_derasterize(
    lib: *const ffi::c_void,
    voxels: *const VoxelGrid,
) -> OctnodeArray {
    let lib = unsafe { &*lib.cast::<LibOptoctrees>() };
    let voxels = unsafe { &*voxels };

    let mut buf = optoctrees::ReverseOctreeBuffer::new();
    let nodes = optoctrees::derasterize(&mut buf, voxels);

    let ptr = (lib.alloc_octnode_array)(nodes.len());
    if ptr.is_null() { panic!("failed to allocate octnode array") }

    unsafe { ptr::copy_nonoverlapping(nodes.as_ptr(), ptr, nodes.len()) }
    OctnodeArray { ptr, len: nodes.len() }
}

#[repr(C)]
pub struct RelativeBlock {
    x: isize,
    y: isize,
    z: isize,
}

#[repr(i32)]
pub enum VoxelOperation {
    AddSphere = 0,
    AddPyramid = 1,
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn optoctree_voxel_op(
    _: *const ffi::c_void,
    voxels: *mut VoxelGrid,
    center: RelativeBlock,
    op: VoxelOperation,
    op_data: *const ffi::c_void,
) {
    let voxels = unsafe { &mut *voxels };
    match op {
        VoxelOperation::AddSphere => {
            let op = unsafe { *op_data.cast::<optoctrees::AddSphere>() };
            optoctrees::voxel_op(
                voxels,
                [center.x, center.y, center.z],
                op,
            );
        },
        VoxelOperation::AddPyramid => {
            let op = unsafe { *op_data.cast::<optoctrees::AddPyramid>() };
            optoctrees::voxel_op(
                voxels,
                [center.x, center.y, center.z],
                op,
            );
        },
    }
}

#[cfg(target_env = "musl")]
#[global_allocator]
static ALLOC: tikv_jemallocator::Jemalloc = tikv_jemallocator::Jemalloc;
