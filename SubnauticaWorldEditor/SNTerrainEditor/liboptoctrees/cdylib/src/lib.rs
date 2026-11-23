use std::{ffi, panic, ptr, slice};
use optoctrees::sn_terrain;

#[repr(C)]
pub struct InitTable {
    pub alloc: unsafe extern "C" fn(size: i64, align: i32) -> *mut ffi::c_void,
    pub free: unsafe extern "C" fn(ptr: *mut ffi::c_void),
    pub log: unsafe extern "C" fn(level: LogLevel, ptr: *const u8, len: i32),
    pub flush_log: unsafe extern "C" fn(),
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

    pub get_octree_blocking: unsafe extern "C" fn(
        lib: *const ffi::c_void,
        pos: sn_terrain::OctreePos,
    ) -> sn_terrain::ffi::SharedOctreePtr,

    pub drop_octree: unsafe extern "C" fn(
        lib: *const ffi::c_void,
        octree: sn_terrain::ffi::SharedOctreePtr,
    ),
}

#[derive(Copy, Clone)]
#[repr(i32)]
pub enum LogLevel {
    Debug = 0,
    Info = 1,
    Warning = 2,
    Error = 3,
    Fatal = 4,
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

    panic::set_hook(Box::new(move |info| {
        let msg = match (info.location(), info.payload_as_str()) {
            (Some(at), Some(msg)) => format!("panic at {at} with: {msg}"),
            (None, Some(msg)) => format!("panic with: {msg}"),
            (Some(at), None) => format!("panic at {at}"),
            (None, None) => "panic".to_owned(),
        };

        unsafe { (init.log)(LogLevel::Fatal, msg.as_ptr(), msg.len() as i32); }
        unsafe { (init.flush_log)(); }
    }));

    let game = match sn_terrain::ffi::current_process_game_dir() {
        Ok(game) => game,
        Err(e) => {
            eprintln!("[liboptoctrees] error: {e}");
            return ptr::null_mut()
        },
    };
    let world = sn_terrain::editor::WorldCache::new(game);

    let lib = LibOptoctrees { table: init, alloc, world };
    let lib = Box::into_raw(Box::new(lib)).cast::<ffi::c_void>();

    let table = LibTable {
        lib, drop: drop_lib,
        octree_voxel_op,
        get_octree_blocking,
        drop_octree,
    };

    Box::into_raw(Box::new(table))
}

struct LibOptoctrees {
    #[expect(unused)] table: InitTable,
    alloc: sn_terrain::ffi::UnityAlloc,
    world: sn_terrain::editor::WorldCache,
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

unsafe extern "C" fn get_octree_blocking(
    lib: *const ffi::c_void,
    pos: sn_terrain::OctreePos,
) -> sn_terrain::ffi::SharedOctreePtr {
    let lib = unsafe { &*lib.cast::<LibOptoctrees>() };
    sn_terrain::ffi::SharedOctreeExt::into_ptr(lib.world.get_octree_blocking(pos))
}

unsafe extern "C" fn drop_octree(
    _: *const ffi::c_void,
    octree: sn_terrain::ffi::SharedOctreePtr,
) {
    let octree = unsafe {
        <sn_terrain::SharedOctree as sn_terrain::ffi::SharedOctreeExt>::from_ptr(octree)
    };

    drop(octree);
}
