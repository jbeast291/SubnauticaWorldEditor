using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Nautilus.Utility;
using SNTerrainEditor.Core.Editing;
using SNTerrainEditor.FileOperations;
using Unity.Collections;
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
                NativeArray<byte> old = octree.octreeBytes;
                NativeGridRasiterizer.FromOctreeIntoGrid(grid, old);
                grid.DEBUG__Clear();
                octree.octreeBytes = NativeGridDerasterizer.ToOctree(grid);
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
            NativeArray<byte> old = octree.octreeBytes;
            NativeGridRasiterizer.FromOctreeIntoGrid(grid, old);
        }
        sw.Stop();
        Plugin.LogError($"(Spheres) Rasterize Took: {sw.ElapsedMilliseconds}ms");
        
        sw.Restart();
        Int3 batchBlockPos = batchIndex * ManagedBatch.OCTREES_PER_SIDE;
        JobHandle prev = default;
        foreach (var kVp in grids) {
            Int3 gridBlockPos = batchBlockPos + kVp.Key;
            SdfSphereEdit edit = new(kVp.Value, gridBlockPos + new Int3(16, 16, 16), gridBlockPos);
            prev = edit.Schedule(prev);
        }
        prev.Complete();
        sw.Stop();
        Plugin.LogError($"(Spheres) Voxel Opp: {sw.ElapsedMilliseconds}ms");
        
        sw.Restart();
        foreach (Int3 octreeLocalIndex in Int3.Range(ManagedBatch.OCTREES_PER_SIDE)) {
            ManagedOctree octree = batch.octrees.Get(octreeLocalIndex);
            NativeArray<byte> old = octree.octreeBytes;
            NativeGrid grid = grids[octreeLocalIndex];
            octree.octreeBytes = NativeGridDerasterizer.ToOctree(grid);
            old.Dispose();
            EDIT_GRID_POOl.Push(grid);
        }
        sw.Stop();
        Plugin.LogError($"(Spheres) Derasterize Took: {sw.ElapsedMilliseconds}ms");
    }
    
    public void Dispose()
    {
        
    }
}