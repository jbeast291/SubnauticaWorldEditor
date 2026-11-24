using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using BepInEx.Logging;
using SNTerrainEditor.Core.DataTypes;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace SNTerrainEditor;

/// <summary>Interop with Rust native library for efficient octree operations.</summary>
public unsafe class LibOptoctrees : IDisposable {
    /// <summary>Draw a sphere on an octree, producing a new octree.</summary>
    /// <SAFETY>The provided <paramref name="octree"/> must remain valid for reads and completely
    /// unmodified until this call returns.</SAFETY>
    internal static unsafe NativeArray<byte> OctreeAddSphere(
        NativeArray<byte> octree,
        Int3 gridBlock, Int3 opBlockCenter, int radius
    ) {
        Octree octnodes = new() {
            ptr = (byte*)octree.GetUnsafePtr(),
            len = (nuint)octree.Length / 4
        };

        Int3 relativeBlock = opBlockCenter - gridBlock;

        Vector3F32 relativeCenter = new() {
            x = relativeBlock.x,
            y = relativeBlock.y,
            z = relativeBlock.z
        };
        AddSphere sphere = new() { radius = radius };

        var octree_voxel_op = Marshal
            .GetDelegateForFunctionPointer<octree_voxel_op>(INSTANCE.table->octree_voxel_op);

        Octree result = octree_voxel_op(
            INSTANCE.table->lib,
            octnodes,
            relativeCenter, VoxelOpDiscrim.AddSphere, &sphere
        );

        return NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(
            result.ptr, (int)result.len * 4, Allocator.Persistent
        );
    }

    //TODO: SAFTEY docs
    internal static unsafe NativeArray<byte> GetOctreeBlocking(Int3 octreeId)
    {
        var get_octree_blocking = Marshal.GetDelegateForFunctionPointer<get_octree_blocking>(
                INSTANCE.table->get_octree_blocking
        );

        var sharedOctreePtr = get_octree_blocking(
            INSTANCE.table->lib, 
            new OctreePos {
                x = octreeId.x,
                y = octreeId.y,
                z = octreeId.z,
            }
        );

        var unityOctree = new NativeArray<byte>(
            sharedOctreePtr.len,
            Allocator.Persistent,
            NativeArrayOptions.UninitializedMemory
        );

        Plugin.LogError("Before MemCpy");
        Logger.Listeners.OfType<DiskLogListener>().ForEach(l => l.LogWriter?.Flush());
        UnsafeUtility.MemCpy(unityOctree.GetUnsafePtr(), sharedOctreePtr.ptr, sharedOctreePtr.len);
        Plugin.LogError("After MemCpy");
        Logger.Listeners.OfType<DiskLogListener>().ForEach(l => l.LogWriter?.Flush());

        var drop_octree = Marshal.GetDelegateForFunctionPointer<drop_octree>(
            INSTANCE.table->drop_octree
        );
        drop_octree(INSTANCE.table->lib, sharedOctreePtr);

        return unityOctree;
    }

    private static LibOptoctrees INSTANCE;
    private readonly LibTable* table;
    private readonly ManualLogSource bepInExLogger;

    static LibOptoctrees() {
        UNITY_ALLOC = UnityAlloc;
        UNITY_FREE = UnityFree;
        BEPINEX_LOG = BepInExLog;
        BEPINEX_FLUSH_LOG = BepInExFlushLog;
        INSTANCE = new();
    }

    private LibOptoctrees() {
        InitTable init = new() {
            alloc = Marshal.GetFunctionPointerForDelegate(UNITY_ALLOC),
            free = Marshal.GetFunctionPointerForDelegate(UNITY_FREE),
            log = Marshal.GetFunctionPointerForDelegate(BEPINEX_LOG),
            flush_log = Marshal.GetFunctionPointerForDelegate(BEPINEX_FLUSH_LOG),
        };
        table = init_liboptoctrees(&init);
        if (table == null) {
            throw new Exception("Native rust code failed to load library");
        }
        bepInExLogger = Logger.CreateLogSource("LibOptoctrees");
    }

    [DllImport("optoctrees", CallingConvention = CallingConvention.Cdecl)]
    private static extern LibTable* init_liboptoctrees(InitTable* init);

    [StructLayout(LayoutKind.Sequential)]
    private struct InitTable {
        internal nint alloc;
        internal nint free;
        internal nint log;
        internal nint flush_log;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LibTable {
        internal void* lib;
        internal nint drop;
        internal nint octree_voxel_op;
        internal nint get_octree_blocking;
        internal nint drop_octree;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void drop(LibTable* libTable);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate Octree octree_voxel_op(
        void* lib,
        Octree octree,
        Vector3F32 center, VoxelOpDiscrim discrim, void* op
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate SharedOctreePtr get_octree_blocking(void* lib, OctreePos pos);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void drop_octree(void* lib, SharedOctreePtr octree);

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

    [StructLayout(LayoutKind.Sequential)]
    private struct Octree {
        internal byte* ptr;
        internal nuint len;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct OctreePos {
        internal int x;
        internal int y;
        internal int z;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SharedOctreePtr {
        internal byte* ptr;
        internal int len;
    }

    private enum LogLevel {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Fatal = 4,
    }

    private static readonly UnityAllocDelegate UNITY_ALLOC;
    private static readonly UnityFreeDelegate UNITY_FREE;
    private static readonly LogDelegate BEPINEX_LOG;
    private static readonly FlushLogDelegate BEPINEX_FLUSH_LOG;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void* UnityAllocDelegate(long size, int align);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void UnityFreeDelegate(void* ptr);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void LogDelegate(LogLevel level, byte* ptr, int len);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void FlushLogDelegate();

    private static void* UnityAlloc(long size, int align) => UnsafeUtility.Malloc(
        size: size,
        alignment: align,
        allocator: Allocator.Persistent
    );

    private static void UnityFree(void* ptr) => UnsafeUtility.Free(
        memory: ptr,
        allocator: Allocator.Persistent
    );

    private static void BepInExLog(LogLevel level, byte* ptr, int len)
    {
        string logMessage = Encoding.UTF8.GetString(ptr, len);
        switch (level) {
            case LogLevel.Fatal: INSTANCE.bepInExLogger.LogFatal(logMessage); break;
            case LogLevel.Error: INSTANCE.bepInExLogger.LogError(logMessage); break;
            case LogLevel.Warning: INSTANCE.bepInExLogger.LogWarning(logMessage); break;
            case LogLevel.Info: INSTANCE.bepInExLogger.LogInfo(logMessage); break;
            case LogLevel.Debug: INSTANCE.bepInExLogger.LogDebug(logMessage); break;
        }
    }
    private static void BepInExFlushLog() 
        => Logger.Listeners.OfType<DiskLogListener>().ForEach(l => l.LogWriter?.Flush());

    public void Dispose()
    {
        var drop = Marshal.GetDelegateForFunctionPointer<drop>(table->drop);
        drop(table);
    }
}
