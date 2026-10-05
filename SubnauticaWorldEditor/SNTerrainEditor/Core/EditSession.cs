using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Nautilus.Utility;
using SNTerrainEditor.Core.Editing;
using SNTerrainEditor.FileOperations;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine.Yoga;
using WorldStreaming;

namespace SNTerrainEditor.Core.DataTypes;

/// <summary>
/// Holds the currently editable voxel terrain for the active save.
/// </summary>
internal class EditSession : IDisposable
{
    
    /*
    Design principles:
        1. Terrain must always be editable regardless of if its visually loaded or not. Treat terrain edits as independent of visuals
        2. Edit sessions should not be concerned with the state of the world streamer. Some other class should be delegated to sync batch loads and refresh 
    
     
    Initializing:
        Load octrees from separate streamer (steal from TP), tho pooling on it isn't necessarily needed?. This will help when data is needed from farther batches later :/
        When octree data is requested from world streamer, and it's a managed octree, use our copy
            if the world streamer already has the octree loaded, then swap in ours and return the array to pool
    
    Editing:
        need to make some kind of generic "edit action"
            NeedsNeighbors: Bool // load surrounding octrees when acting. This is also a hard limitation for performance,
                                    an edit *cannot* need neighbors greater than 32 voxels away (this is VERY reasonable)
            Center: Int3 // block location of center
     */
    
    internal const int BatchSideLength = 160;
    internal const int GridsPerBatch = 5;
    
    private readonly WorldStreamer WORLD_STREAMER;

    private const int GRID_POOL_SIZE = 128;
    private static readonly Stack<NativeGrid> EDIT_GRID_POOl = new();
    
    private readonly Dictionary<Int3, ManagedBatch> managedBatches = new();
    
    internal EditSession(WorldStreamer worldStreamer) {
        WORLD_STREAMER = worldStreamer;
        
        for(int i = 0; i < GRID_POOL_SIZE; i++) {
            EDIT_GRID_POOl.Push(new NativeGrid());
        }
    }

    internal void AddBatch(Int3 batchIndex) {
        managedBatches.Add(batchIndex, OctreeReading.GetBatchOctrees(batchIndex));
    }

    internal ManagedOctree GetBatchOctree(Int3 batchIndex, Int3 octreeIndex) {
        return managedBatches[batchIndex].octrees.Get(octreeIndex);
    }
    
    internal void DEBUG__Clear() {
        NativeGrid grid = EDIT_GRID_POOl.Pop();
        
        foreach (ManagedBatch batch in managedBatches.Values) {
            foreach (ManagedOctree octree in batch.octrees) {
                NativeArray<OctNode> old = octree.arr;
                LibOptoctrees.Rasterize(old, grid);
                grid.DEBUG__Clear();
                NativeArray<OctNode> octreeBytes = LibOptoctrees.Derasterize(grid);
                octree.arr = octreeBytes;
                old.Dispose();
            }
        }
        EDIT_GRID_POOl.Push(grid);
    }
    
    internal void DEBUG__Sphere(Int3 batchIndex) {
        ManagedBatch batch = managedBatches[batchIndex];
        System.Diagnostics.Stopwatch sw = new();
        sw.Start();
        Dictionary<Int3, NativeGrid> grids = new();
        foreach (Int3 octreeLocalIndex in Int3.Range(ManagedBatch.OCTREES_PER_SIDE)) {
            NativeGrid grid = EDIT_GRID_POOl.Pop();
            grids.Add(octreeLocalIndex, grid);
            ManagedOctree octree = batch.octrees.Get(octreeLocalIndex);
            NativeArray<OctNode> old = octree.arr;
            LibOptoctrees.Rasterize(old, grid);
        }
        sw.Stop();
        Plugin.LogError($"(Spheres) Average Rasterize Took: {sw.Elapsed.TotalMilliseconds / 125.0}ms");
        
        sw.Restart();
        Int3 batchBlockPos = batchIndex * ManagedBatch.OCTREES_PER_SIDE;
        foreach (var kVp in grids) {
            Int3 gridBlockPos = batchBlockPos + (kVp.Key * NativeGrid.SideLength);
            LibOptoctrees.VoxelOpAddSphere(kVp.Value, gridBlockPos, gridBlockPos + new Int3(16), 16);
        }
        sw.Stop();
        Plugin.LogError($"(Spheres) Average Voxel Opp: {sw.Elapsed.TotalMilliseconds / 125.0}ms");
        
        sw.Restart();
        foreach (Int3 octreeLocalIndex in Int3.Range(ManagedBatch.OCTREES_PER_SIDE)) {
            ManagedOctree octree = batch.octrees.Get(octreeLocalIndex);
            NativeArray<OctNode> old = octree.arr;
            NativeGrid grid = grids[octreeLocalIndex];
            
            octree.arr =  LibOptoctrees.Derasterize(grid);
            old.Dispose();
            EDIT_GRID_POOl.Push(grid);
        }
        sw.Stop();
        Plugin.LogError($"(Spheres) Average Derasterize Took: {sw.Elapsed.TotalMilliseconds / 125.0}ms");
    }

    public void DEBUG__dumpTestData(Int3 batchIndex) {
        NativeGrid grid = EDIT_GRID_POOl.Pop();
        
        ManagedBatch batch = managedBatches[batchIndex];
        ManagedOctree octree = batch.octrees.Get(new(0));
        
        NativeArray<OctNode> old = octree.arr;
        LibOptoctrees.Rasterize(old, grid);

        WriteNativeArrayToFile(old, "sphereOctree.bin");
        WriteNativeArrayToFile(grid.nativeArray, "sphereGrid.bin");
        
        EDIT_GRID_POOl.Push(grid);
    }
    
    
    private static void WriteNativeArrayToFile<T>(NativeArray<T> nativeArray, string fileName) where T : struct {
        NativeArray<byte> unsafeArray = nativeArray.Reinterpret<byte>(UnsafeUtility.SizeOf<T>());
        byte[] managedBuffer = new byte[unsafeArray.Length];
        unsafeArray.CopyTo(managedBuffer);
        string modFolder = Plugin.GetModDirectory();
        File.WriteAllBytes(Path.Combine(modFolder, fileName), managedBuffer);
    }
    
    public void Dispose()
    {
        
    }
}