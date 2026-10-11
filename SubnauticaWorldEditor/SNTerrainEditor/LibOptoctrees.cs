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
        internal nint alloc_octree;
        internal nint free_octree;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void drop(LibTable libTable);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void rasterize_octree(void* lib, OctnodeArray octrees, Voxel* voxels);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void voxel_op(void* lib, Voxel* voxels, Vector3Float32 center, VoxelOperation discrim, void* op);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate OctnodeArray derasterize_octree(void* lib, Voxel* voxels);

    [StructLayout(LayoutKind.Sequential)]
    private struct LibTable {
        internal void* lib;
        internal nint drop;
        internal nint rasterize_octree;
        internal nint voxel_op;
        internal nint derasterize_octree;
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
        var allocOctree = Marshal.GetFunctionPointerForDelegate(ALLOC_OCTREE);

        var freeOctree = Marshal.GetFunctionPointerForDelegate(FREE_OCTREE);

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

        var rasterize_octree = Marshal.GetDelegateForFunctionPointer<rasterize_octree>(
            TABLE->rasterize_octree
        );
        rasterize_octree(TABLE->lib, arr, grid.GridPtr);
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

        var voxel_op = Marshal.GetDelegateForFunctionPointer<voxel_op>(TABLE->voxel_op);
        voxel_op(TABLE->lib, grid.GridPtr, relativeCenter, VoxelOperation.AddSphere, &sphere);
    }

    /// <summary>Derasterizes a dense voxel grid into a a sparse voxel octree.</summary>
    /// <param name="grid">32x32x32 grid of voxels</param>
    /// <returns>A unmanaged array allocated by unity's allocator of <see cref="OctNode"/>s</returns>
    /// <SAFTEY>See <see cref="LibOptoctrees"/>' SAFTEY documentation for struct alignment requirements.<br/>
    /// The <paramref name="grid"/> must be already initialized and remain allocated for the duration
    /// of the function call</SAFTEY>
    internal static unsafe NativeArray<OctNode> Derasterize(NativeGrid grid) {
        var derasterize_octree = Marshal.GetDelegateForFunctionPointer<derasterize_octree>(
            TABLE->derasterize_octree
        );

        OctnodeArray result = derasterize_octree(TABLE->lib, grid.GridPtr);

        return NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<OctNode>(
            result.ptr, (int)result.len, Allocator.Persistent
        );
    }
}
