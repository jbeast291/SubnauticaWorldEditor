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
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate OctNode* AllocOctNodesDelegate(nuint count);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] 
    private delegate void FreeOctNodesDelegate(OctNode* ptr, nuint count);

    private static OctNode* AllocOctNodes(nuint count) {
        long size = checked((long)count * UnsafeUtility.SizeOf<OctNode>());
        return (OctNode*)UnsafeUtility.Malloc(size, UnsafeUtility.AlignOf<OctNode>(), Allocator.Persistent);
    }
    private static void FreeOctNodes(OctNode* ptr, nuint count) => UnsafeUtility.Free(ptr, Allocator.Persistent);

    private static readonly AllocOctNodesDelegate Alloc = AllocOctNodes;
    private static readonly FreeOctNodesDelegate Free = FreeOctNodes;
    
    [StructLayout(LayoutKind.Sequential)]
    private struct OctnodeArray {
        internal OctNode* ptr;
        internal nuint len;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    private struct Vec3Int32 {
        internal int x;
        internal int y;
        internal int z;
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
    private static extern void* new_liboptoctrees(AllocOctNodesDelegate alloc, FreeOctNodesDelegate free);

    [DllImport("optoctrees", CallingConvention = CallingConvention.Cdecl)]
    private static extern void drop_liboptoctrees(void* lib);

    [DllImport("optoctrees", CallingConvention = CallingConvention.Cdecl)]
    private static extern void optoctree_rasterize(void* lib, OctnodeArray octree, Voxel* voxels);
    
    [DllImport("optoctrees", CallingConvention = CallingConvention.Cdecl)]
    private static extern void optoctree_voxel_op(
        void* lib, 
        Voxel* voxels, 
        Vec3Int32 center, 
        VoxelOperation discrim, 
        void* op
    );
    
    [DllImport("optoctrees", CallingConvention = CallingConvention.Cdecl)]
    private static extern OctnodeArray optoctree_derasterize(void* lib, Voxel* voxels);
    
    private static readonly void* LIB = new_liboptoctrees(Alloc, Free);

    /// <summary>Rasterizes a sparse voxel octree into a given dense voxel grid.</summary>
    /// <param name="octree">A sequential unmanaged array of <see cref="OctNode"/>s following
    /// the games format</param>
    /// <param name="grid">32x32x32 grid of points</param>
    /// <SAFTEY>See <see cref="LibOptoctrees"/>' SAFTEY documentation for struct alignment requirements.<br/>
    /// Both the <paramref name="octree"/> and <paramref name="grid"/> must stay allocated for the
    /// duration of the function call. The <paramref name="octree"/> must be of a valid structure
    /// so that the grid will be fully initialized after.</SAFTEY>
    internal static unsafe void Rasterize(NativeArray<OctNode> octree, NativeGrid grid) {
        OctnodeArray arr = new() {
            ptr = (OctNode*)octree.GetUnsafePtr(),
            len = (nuint)octree.Length
        };
        optoctree_rasterize(LIB, arr, grid.GridPtr);
    }

    internal static unsafe void VoxelOpAddSphere(NativeGrid grid, Int3 gridBlock, Int3 center, int radius) {
        Int3 relativeBlock = center - gridBlock;

        Vec3Int32 relativeCenter = new() { 
            x = relativeBlock.x,
            y = relativeBlock.y,
            z = relativeBlock.z
        };
        AddSphere sphere = new() { scale = radius };
        
        optoctree_voxel_op(LIB, grid.GridPtr, relativeCenter, VoxelOperation.AddSphere, &sphere);
    }
    
    /// <summary>Derasterizes a dense voxel grid into a a sparse voxel octree.</summary>
    /// <param name="grid">32x32x32 grid of voxels</param>
    /// <returns>A unmanaged array allocated by unity's allocator of <see cref="OctNode"/>s</returns>
    /// <SAFTEY>See <see cref="LibOptoctrees"/>' SAFTEY documentation for struct alignment requirements.<br/>
    /// The <paramref name="grid"/> must be already initialized and remain allocated for the duration
    /// of the function call</SAFTEY>
    internal static unsafe NativeArray<OctNode> Derasterize(NativeGrid grid) {
        OctnodeArray result = optoctree_derasterize(LIB, grid.GridPtr);
        return NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<OctNode>(result.ptr, (int)result.len, Allocator.Persistent);
    }
}