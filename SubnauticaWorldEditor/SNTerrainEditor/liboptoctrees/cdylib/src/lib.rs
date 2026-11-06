use std::{alloc::{self, Allocator}, ffi, num, ptr, slice};
use optoctrees::{Octnode, VoxelGrid};

#[repr(C)]
pub struct InitTable {
    pub alloc: unsafe extern "C" fn(size: i64, align: i32) -> *mut ffi::c_void,
    pub free: unsafe extern "C" fn(ptr: *mut ffi::c_void),
}

#[repr(C)]
pub struct LibTable {
    pub lib: *mut ffi::c_void,
    pub drop: unsafe extern "C" fn(table: *mut LibTable),

    pub rasterize_octree: unsafe extern "C" fn(
        lib: *const ffi::c_void,
        octree: OctnodeArray,
    ) -> *mut VoxelGrid,

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
    let alloc = LibAlloc { alloc: init.alloc, free: init.free };
    let lib = LibOptoctrees { table: init, alloc };
    let lib = Box::into_raw(Box::new(lib)).cast::<ffi::c_void>();

    let table = LibTable {
        lib, drop: drop_lib,
        rasterize_octree,
        voxel_op,
        derasterize_octree,
    };

    Box::into_raw(Box::new(table))
}

struct LibOptoctrees {
    #[expect(unused)] table: InitTable,
    alloc: LibAlloc,
}

unsafe extern "C" fn drop_lib(table: *mut LibTable) {
    let table = unsafe { Box::from_raw(table) };
    let lib = unsafe { Box::from_raw(table.lib.cast::<LibOptoctrees>()) };
    drop(lib);
    drop(table);
}

unsafe extern "C" fn rasterize_octree(
    lib: *const ffi::c_void,
    octree: OctnodeArray,
) -> *mut VoxelGrid {
    let lib = unsafe { &*lib.cast::<LibOptoctrees>() };
    let octree = unsafe { slice::from_raw_parts(octree.ptr, octree.len) };

    let voxels = optoctrees::rasterize(octree, lib.alloc);
    let (voxels, _) = Box::into_raw_with_allocator(voxels);

    voxels
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

    let nodes = optoctrees::derasterize(voxels, lib.alloc);
    let (nodes, _) = Box::into_raw_with_allocator(nodes);

    OctnodeArray { ptr: nodes.cast::<Octnode>(), len: nodes.len() }
}

#[derive(Copy, Clone)]
struct LibAlloc {
    alloc: unsafe extern "C" fn(size: i64, align: i32) -> *mut ffi::c_void,
    free: unsafe extern "C" fn(ptr: *mut ffi::c_void),
}

unsafe impl Allocator for LibAlloc {
    fn allocate(&self, layout: alloc::Layout) -> Result<ptr::NonNull<[u8]>, alloc::AllocError> {
        let align = num::NonZero::new(layout.align()).unwrap_or(num::NonZero::<usize>::MIN);

        if layout.size() == 0 {
            let dangling = ptr::NonNull::without_provenance(align);
            Ok(ptr::NonNull::slice_from_raw_parts(dangling, 0))
        } else {
            let size = layout.size().try_into().map_err(|_| alloc::AllocError)?;
            let align = align.get().try_into().map_err(|_| alloc::AllocError)?;

            let ptr = unsafe { (self.alloc)(size, align) }.cast::<u8>();
            let ptr = ptr::NonNull::new(ptr).ok_or(alloc::AllocError)?;
            Ok(ptr::NonNull::slice_from_raw_parts(ptr, layout.size()))
        }
    }

    unsafe fn deallocate(&self, ptr: ptr::NonNull<u8>, layout: alloc::Layout) {
        if layout.size() != 0 {
            unsafe { (self.free)(ptr.as_ptr().cast::<ffi::c_void>()) }
        }
    }
}
