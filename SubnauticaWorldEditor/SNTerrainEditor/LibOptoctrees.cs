using System.Runtime.InteropServices;
using SNTerrainEditor.Core.DataTypes;
using SNTerrainEditor.Core.Editing;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace SNTerrainEditor;

/// <summary>Interop with Rust native library for efficient octree operations.</summary>
public static unsafe class LibOptoctrees {
    // TODO: switch to a non-static class to better support fallible initialization and drop

    /// <summary>Draw a sphere on an octree, producing a new octree.</summary>
    /// <SAFETY>The provided <paramref name="octree"/> must remain valid for reads and completely
    /// unmodified until this call returns.</SAFETY>
    internal static unsafe NativeArray<OctNode> OctreeAddSphere(
        NativeArray<OctNode> octree,
        Int3 gridBlock, Int3 opBlockCenter, int radius
    ) {
        Octree octnodes = new() {
            ptr = (OctNode*)octree.GetUnsafePtr(),
            len = (nuint)octree.Length
        };

        Int3 relativeBlock = opBlockCenter - gridBlock;

        Vector3F32 relativeCenter = new() {
            x = relativeBlock.x,
            y = relativeBlock.y,
            z = relativeBlock.z
        };
        AddSphere sphere = new() { radius = radius };

        var octree_voxel_op = Marshal
            .GetDelegateForFunctionPointer<octree_voxel_op>(TABLE->octree_voxel_op);

        Octree result = octree_voxel_op(
            TABLE->lib,
            octnodes,
            relativeCenter, VoxelOpDiscrim.AddSphere, &sphere
        );

        return NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<OctNode>(
            result.ptr, (int)result.len, Allocator.Persistent
        );
    }

    private static readonly LibTable* TABLE;

    static LibOptoctrees() {
        var unityAlloc = Marshal.GetFunctionPointerForDelegate(UNITY_ALLOC);

        var unityFree = Marshal.GetFunctionPointerForDelegate(UNITY_FREE);

        InitTable init = new() {
            alloc = unityAlloc,
            free = unityFree,
        };
        TABLE = init_liboptoctrees(&init);
    }

    [DllImport("optoctrees", CallingConvention = CallingConvention.Cdecl)]
    private static extern LibTable* init_liboptoctrees(InitTable* init);

    [StructLayout(LayoutKind.Sequential)]
    private struct InitTable {
        internal nint alloc;
        internal nint free;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LibTable {
        internal void* lib;
        internal nint drop;
        internal nint octree_voxel_op;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void drop(LibTable* libTable);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate Octree octree_voxel_op(
        void* lib,
        Octree octree,
        Vector3F32 center, VoxelOpDiscrim discrim, void* op
    );

    [StructLayout(LayoutKind.Sequential)]
    private struct Octree {
        internal OctNode* ptr;
        internal nuint len;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Vector3F32 {
        internal float x;
        internal float y;
        internal float z;
    }

    private enum VoxelOpDiscrim {
        AddSphere = 0,
        AddPyramid = 1,
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AddSphere {
        internal float radius;
    }

    private static readonly UnityAllocDelegate UNITY_ALLOC = UnityAlloc;
    private static readonly UnityFreeDelegate UNITY_FREE = UnityFree;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void* UnityAllocDelegate(long size, int align);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void UnityFreeDelegate(void* ptr);

    private static void* UnityAlloc(long size, int align) => UnsafeUtility.Malloc(
        size: size,
        alignment: align,
        allocator: Allocator.Persistent
    );

    private static void UnityFree(void* ptr) => UnsafeUtility.Free(
        memory: ptr,
        allocator: Allocator.Persistent
    );
}
