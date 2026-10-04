using System;
using System.Runtime.InteropServices;
using SNTerrainEditor.Core.DataTypes;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace SNTerrainEditor;

//TODO: write safety guarantees for internal api the C# side MUST follow
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

    [DllImport("optoctrees", CallingConvention = CallingConvention.Cdecl)]
    private static extern void* new_liboptoctrees(AllocOctNodesDelegate alloc, FreeOctNodesDelegate free);

    [DllImport("optoctrees", CallingConvention = CallingConvention.Cdecl)]
    private static extern void drop_liboptoctrees(void* lib);

    [DllImport("optoctrees", CallingConvention = CallingConvention.Cdecl)]
    private static extern OctnodeArray optoctree_derasterize(void* lib, Voxel* voxels);

    [DllImport("optoctrees", CallingConvention = CallingConvention.Cdecl)]
    private static extern void optoctree_rasterize(void* lib, OctnodeArray octree, Voxel* voxels);
    
    //TODO: pool liboptoctrees objects so we dont reallocate buffer every time
    //  (pool size should roughly be how many opps we want active at given time)
    internal static void Rasterize(NativeArray<OctNode> octree, NativeGrid grid) {
        void* lib = new_liboptoctrees(Alloc, Free);
        try {
            OctnodeArray arr = new() {
                ptr = (OctNode*)octree.GetUnsafePtr(),
                len = (nuint)octree.Length
            };
            optoctree_rasterize(lib, arr, grid.GridPtr);
        }
        finally { drop_liboptoctrees(lib); }
    }
    
    internal static NativeArray<OctNode> Derasterize(NativeGrid grid) {
        void* lib = new_liboptoctrees(Alloc, Free);
        try {
            OctnodeArray result = optoctree_derasterize(lib, grid.GridPtr);
            return NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<OctNode>(result.ptr, (int)result.len, Allocator.Persistent);
        }
        finally { drop_liboptoctrees(lib); }
    }
}