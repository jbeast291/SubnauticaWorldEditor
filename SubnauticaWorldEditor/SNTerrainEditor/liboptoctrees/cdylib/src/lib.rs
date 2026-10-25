use std::{ffi, ptr, slice};
use optoctrees::{Octnode, VoxelGrid, VoxelGridOutput};

struct LibOptoctrees {
    table: InitTable,
}

#[repr(C)]
pub struct InitTable {
    pub alloc_octree: extern "C" fn(len: usize) -> *mut Octnode,
    pub free_octree: unsafe extern "C" fn(ptr: *mut Octnode, len: usize),
}

#[repr(C)]
pub struct LibTable {
    pub lib: *mut ffi::c_void,
    pub drop: unsafe extern "C" fn(table: *mut LibTable),

    pub rasterize_octree: unsafe extern "C" fn(
        lib: *const ffi::c_void,
        octree: OctnodeArray,
        voxels: *mut VoxelGridOutput,
    ),

    pub voxel_op: unsafe extern "C" fn(
        lib: *const ffi::c_void,
        voxels: *mut VoxelGrid,
        center: Vector3Float32,
        discrim: VoxelOperation,
        op: *const ffi::c_void,
    ),

    pub derasterize_octree: unsafe extern "C" fn(
        lib: *const ffi::c_void,
        voxels: *const VoxelGrid,
    ) -> OctnodeArray,
}

#[repr(C)]
pub struct OctnodeArray {
    pub ptr: *mut Octnode,
    pub len: usize,
}

#[repr(C)]
pub struct Vector3Float32 {
    pub x: f32,
    pub y: f32,
    pub z: f32,
}

#[repr(i32)]
pub enum VoxelOperation {
    AddSphere = 0,
    AddPyramid = 1,
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn init_liboptoctrees(init: *const InitTable) -> *mut LibTable {
    let init = unsafe { ptr::read(init) };
    let lib = LibOptoctrees { table: init };
    let lib = Box::into_raw(Box::new(lib)).cast::<ffi::c_void>();

    let table = LibTable {
        lib, drop: drop_lib,
        rasterize_octree,
        voxel_op,
        derasterize_octree,
    };

    Box::into_raw(Box::new(table))
}

unsafe extern "C" fn drop_lib(table: *mut LibTable) {
    let table = unsafe { Box::from_raw(table) };
    let lib = unsafe { Box::from_raw(table.lib.cast::<LibOptoctrees>()) };
    drop(lib);
    drop(table);
}

unsafe extern "C" fn rasterize_octree(
    _: *const ffi::c_void,
    octree: OctnodeArray,
    voxels: *mut VoxelGridOutput,
) {
    let octree = unsafe { slice::from_raw_parts(octree.ptr, octree.len) };
    let voxels = unsafe { &mut *voxels };

    optoctrees::rasterize(octree, voxels);
}

unsafe extern "C" fn voxel_op(
    _: *const ffi::c_void,
    voxels: *mut VoxelGrid,
    center: Vector3Float32,
    discrim: VoxelOperation,
    op: *const ffi::c_void,
) {
    let voxels = unsafe { &mut *voxels };
    match discrim {
        VoxelOperation::AddSphere => {
            let op = unsafe { *op.cast::<optoctrees::AddSphere>() };
            optoctrees::voxel_op(
                voxels,
                [center.x, center.y, center.z],
                op,
            );
        },
        VoxelOperation::AddPyramid => {
            let op = unsafe { *op.cast::<optoctrees::AddPyramid>() };
            optoctrees::voxel_op(
                voxels,
                [center.x, center.y, center.z],
                op,
            );
        },
    }
}

unsafe extern "C" fn derasterize_octree(
    lib: *const ffi::c_void,
    voxels: *const VoxelGrid,
) -> OctnodeArray {
    let lib = unsafe { &*lib.cast::<LibOptoctrees>() };
    let voxels = unsafe { &*voxels };

    let mut buf = optoctrees::ReverseOctreeBuffer::new();
    let nodes = optoctrees::derasterize(&mut buf, voxels);

    let ptr = (lib.table.alloc_octree)(nodes.len());
    if ptr.is_null() { panic!("failed to allocate octnode array") }

    unsafe { ptr::copy_nonoverlapping(nodes.as_ptr(), ptr, nodes.len()) }
    OctnodeArray { ptr, len: nodes.len() }
}
