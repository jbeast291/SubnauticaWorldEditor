use std::{ffi, ptr, slice};
use optoctrees::sn_terrain;

#[repr(C)]
pub struct InitTable {
    pub alloc: unsafe extern "C" fn(size: i64, align: i32) -> *mut ffi::c_void,
    pub free: unsafe extern "C" fn(ptr: *mut ffi::c_void),
}

#[repr(C)]
pub struct LibTable {
    pub lib: *mut ffi::c_void,
    pub drop: unsafe extern "C" fn(table: *mut LibTable),

    pub octree_voxel_op: unsafe extern "C" fn(
        lib: *const ffi::c_void,
        octree: Octree,
        center: Vector3F32,
        discrim: VoxelOpDiscrim,
        op: *const ffi::c_void,
    ) -> Octree,
}

#[repr(C)]
pub struct Octree {
    pub ptr: *mut sn_terrain::Octnode,
    pub len: usize,
}

#[repr(C)]
pub struct Vector3F32 {
    pub x: f32,
    pub y: f32,
    pub z: f32,
}

#[repr(i32)]
pub enum VoxelOpDiscrim {
    AddSphere = 0,
    AddPyramid = 1,
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn init_liboptoctrees(init: *const InitTable) -> *mut LibTable {
    let init = unsafe { ptr::read(init) };
    let alloc = unsafe { sn_terrain::ffi::UnityAlloc::new(init.alloc, init.free) };
    let lib = LibOptoctrees { table: init, alloc };
    let lib = Box::into_raw(Box::new(lib)).cast::<ffi::c_void>();

    let table = LibTable {
        lib, drop: drop_lib,
        octree_voxel_op,
    };

    Box::into_raw(Box::new(table))
}

struct LibOptoctrees {
    #[expect(unused)] table: InitTable,
    alloc: sn_terrain::ffi::UnityAlloc,
}

unsafe extern "C" fn drop_lib(table: *mut LibTable) {
    let table = unsafe { Box::from_raw(table) };
    let lib = unsafe { Box::from_raw(table.lib.cast::<LibOptoctrees>()) };
    drop(lib);
    drop(table);
}

unsafe extern "C" fn octree_voxel_op(
    lib: *const ffi::c_void,
    octree: Octree,
    center: Vector3F32,
    discrim: VoxelOpDiscrim,
    op: *const ffi::c_void,
) -> Octree {
    let lib = unsafe { &*lib.cast::<LibOptoctrees>() };
    let octree = unsafe { slice::from_raw_parts(octree.ptr, octree.len) };
    let octree = sn_terrain::Octree::new(octree);
    let center = [center.x, center.y, center.z];

    let octree = match discrim {
        VoxelOpDiscrim::AddSphere => {
            let op = unsafe { *op.cast::<optoctrees::AddSphere>() };
            optoctrees::octree_voxel_op(octree, center, op, lib.alloc)
        },
        VoxelOpDiscrim::AddPyramid => {
            let op = unsafe { *op.cast::<optoctrees::AddPyramid>() };
            optoctrees::octree_voxel_op(octree, center, op, lib.alloc)
        },
    };
    let (octree, _) = Box::into_raw_with_allocator(octree.into_nodes());

    Octree { ptr: octree.cast(), len: octree.len() }
}
