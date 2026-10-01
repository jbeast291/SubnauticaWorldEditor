using System;
using System.Runtime.InteropServices;
using SNTerrainEditor.Core.DataTypes;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace SNTerrainEditor;

public unsafe class liboptoctrees {
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate OctNode* AllocOctNodesDelegate(UIntPtr count);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void FreeOctNodesDelegate(OctNode* ptr, UIntPtr count);
    
    private static OctNode* AllocOctNodes(UIntPtr count) {
        long size = checked((long)count.ToUInt64() * UnsafeUtility.SizeOf<OctNode>());
        return (OctNode*)UnsafeUtility.Malloc(size, UnsafeUtility.AlignOf<OctNode>(), Allocator.Persistent);
    }
    private static void FreeOctNodes(OctNode* ptr, UIntPtr count) => UnsafeUtility.Free(ptr, Allocator.Persistent);
    
    private static readonly AllocOctNodesDelegate Alloc = AllocOctNodes;

    private static readonly FreeOctNodesDelegate Free = FreeOctNodes;
    
    [StructLayout(LayoutKind.Sequential)]
    private struct OctnodeArray {
        public OctNode* ptr;
        public UIntPtr len;
    }

    [DllImport("optoctrees", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr new_liboptoctrees(AllocOctNodesDelegate alloc, FreeOctNodesDelegate free);

    [DllImport("optoctrees", CallingConvention = CallingConvention.Cdecl)]
    private static extern void drop_liboptoctrees(IntPtr lib);

    [DllImport("optoctrees", CallingConvention = CallingConvention.Cdecl)]
    private static extern OctnodeArray optoctree_derasterize(IntPtr lib, Voxel* voxels);
    
    public static NativeArray<OctNode> Derasterize(Voxel* voxels) {
        IntPtr lib = new_liboptoctrees(Alloc, Free);
        try {
            OctnodeArray result = optoctree_derasterize(lib, voxels);
            return NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<OctNode>(result.ptr, (int)result.len.ToUInt32(), Allocator.Persistent);
        }
        finally { drop_liboptoctrees(lib); }
    }
}