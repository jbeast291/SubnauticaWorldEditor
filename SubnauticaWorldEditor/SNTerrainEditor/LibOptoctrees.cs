using System.Runtime.InteropServices;
using SNTerrainEditor.Core.DataTypes;
using SNTerrainEditor.Core.Editing;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace SNTerrainEditor;

/// <summary>
/// Interop with Rust native library for efficient voxel operations
/// </summary>
/// <SAFTEY>
/// Required struct alignments:<br/>
/// <see cref="Voxel"/> must be aligned to ushort (pack = 2)<br/>
/// <see cref="OctNode"/> must be aligned to bytes (pack = 1)<br/>
/// <see cref="NativeGrid"/> must have its internal array's aligned to 32 (pack = 32)<br/>
/// </SAFTEY>
public static unsafe class LibOptoctrees {
    // TODO: switch to a non-static class to better support fallible initialization and drop

    [StructLayout(LayoutKind.Sequential)]
    private struct InitTable {
        internal delegate* unmanaged[Cdecl]<nuint, OctNode*> alloc_octree;
        internal delegate* unmanaged[Cdecl]<OctNode*, nuint, void> free_octree;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LibTable {
        internal void* lib;
        internal delegate* unmanaged[Cdecl]<LibTable*, void> drop;

        internal delegate* unmanaged[Cdecl]<
            void*, // lib
            OctnodeArray, // octree
            Voxel*, // voxels
        void> rasterize_octree;

        internal delegate* unmanaged[Cdecl]<
            void*, // lib
            Voxel*, // voxels
            Vector3Float32, // center
            VoxelOperation, // discrim
            void*, // op
        void> voxel_op;

        internal delegate* unmanaged[Cdecl]<
            void*, // lib
            Voxel*, // voxels
        OctnodeArray> derasterize_octree;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct OctnodeArray {
        internal OctNode* ptr;
        internal nuint len;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Vector3Float32 {
        internal float x;
        internal float y;
        internal float z;
    }

    private enum VoxelOperation {
        AddSphere = 0,
        AddPyramid = 1,
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AddSphere {
        internal float scale;
    }

    [DllImport("optoctrees", CallingConvention = CallingConvention.Cdecl)]
    private static extern LibTable* init_liboptoctrees(InitTable* init);

    private static readonly LibTable* TABLE;

    static LibOptoctrees() {
        var allocOctree = (delegate* unmanaged[Cdecl]<nuint, OctNode*>)
            Marshal.GetFunctionPointerForDelegate(ALLOC_OCTREE);

        var freeOctree = (delegate* unmanaged[Cdecl]<OctNode*, nuint, void>)
            Marshal.GetFunctionPointerForDelegate(FREE_OCTREE);

        InitTable init = new() {
            alloc_octree = allocOctree,
            free_octree = freeOctree,
        };
        TABLE = init_liboptoctrees(&init);
    }

    private static OctNode* AllocOctree(nuint len) => (OctNode*)UnsafeUtility.Malloc(
        size: checked((long)len * UnsafeUtility.SizeOf<OctNode>()),
        alignment: UnsafeUtility.AlignOf<OctNode>(),
        allocator: Allocator.Persistent
    );

    private static void FreeOctree(OctNode* ptr, nuint len) => UnsafeUtility.Free(
        memory: ptr,
        allocator: Allocator.Persistent
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate OctNode* AllocOctreeDelegate(nuint count);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void FreeOctreeDelegate(OctNode* ptr, nuint count);

    private static readonly AllocOctreeDelegate ALLOC_OCTREE = AllocOctree;
    private static readonly FreeOctreeDelegate FREE_OCTREE = FreeOctree;

    /// <summary>Rasterizes a sparse voxel octree into a given dense voxel grid.</summary>
    /// <param name="octree">A sequential unmanaged array of <see cref="OctNode"/>s following
    /// the games format</param>
    /// <param name="grid">32x32x32 grid of points</param>
    /// <SAFTEY>See <see cref="LibOptoctrees"/>' SAFTEY documentation for struct alignment
    /// requirements.<br/>
    /// Both the <paramref name="octree"/> and <paramref name="grid"/> must stay allocated for the
    /// duration of the function call. The <paramref name="octree"/> must be of a valid structure
    /// so that the grid will be fully initialized after.</SAFTEY>
    internal static unsafe void Rasterize(NativeArray<OctNode> octree, NativeGrid grid) {
        OctnodeArray arr = new() {
            ptr = (OctNode*)octree.GetUnsafePtr(),
            len = (nuint)octree.Length
        };
        TABLE->rasterize_octree(TABLE->lib, arr, grid.GridPtr);
    }

    internal static unsafe void VoxelOpAddSphere(
        NativeGrid grid, Int3 gridBlock, Int3 opBlockCenter, int radius
    ) {
        Int3 relativeBlock = opBlockCenter - gridBlock;

        Vector3Float32 relativeCenter = new() {
            x = relativeBlock.x,
            y = relativeBlock.y,
            z = relativeBlock.z
        };
        AddSphere sphere = new() { scale = radius };
        TABLE->voxel_op(
            TABLE->lib, grid.GridPtr, relativeCenter, VoxelOperation.AddSphere, &sphere
        );
    }

    /// <summary>Derasterizes a dense voxel grid into a a sparse voxel octree.</summary>
    /// <param name="grid">32x32x32 grid of voxels</param>
    /// <returns>A unmanaged array allocated by unity's allocator of <see cref="OctNode"/>s</returns>
    /// <SAFTEY>See <see cref="LibOptoctrees"/>' SAFTEY documentation for struct alignment requirements.<br/>
    /// The <paramref name="grid"/> must be already initialized and remain allocated for the duration
    /// of the function call</SAFTEY>
    internal static unsafe NativeArray<OctNode> Derasterize(NativeGrid grid) {
        OctnodeArray result = TABLE->derasterize_octree(TABLE->lib, grid.GridPtr);
        return NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<OctNode>(
            result.ptr, (int)result.len, Allocator.Persistent
        );
    }
}
