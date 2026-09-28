using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using SNTerrainEditor.FileOperations;
using Unity.Collections;
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

    private const int GRID_POOL_SIZE = 64;
    private static readonly BlockingCollection<NativeGrid> EDIT_GRID_POOl = new();
    
    private readonly Dictionary<Int3, ManagedBatch> managedBatches = new();
    
    internal EditSession(WorldStreamer worldStreamer) {
        WORLD_STREAMER = worldStreamer;
        
        for(int i = 0; i < GRID_POOL_SIZE; i++) {
            EDIT_GRID_POOl.Add(new NativeGrid());
        }
    }

    internal void AddBatch(Int3 batchIndex) {
        managedBatches.Add(batchIndex, OctreeReading.GetBatchOctrees(batchIndex));
    }

    internal ManagedOctree GetBatchOctree(Int3 batchIndex, Int3 octreeIndex) {
        return managedBatches[batchIndex].octrees.Get(octreeIndex);
    }
    
    internal void DEBUG__ModifyAllLava() {
        NativeGrid grid = EDIT_GRID_POOl.Take();
        
        foreach (ManagedBatch batch in managedBatches.Values) {
            foreach (ManagedOctree octree in batch.octrees) {
                grid.SetFromOctree(octree.octreeBytes);
                grid.DEBUG__ModifyWithLavaTexture();
                octree.octreeBytes.Dispose();
                octree.octreeBytes = grid.DerasterizeToOctree();
            }
        }
    }

    internal void EndSession() => Dispose(); 
    
    public void Dispose()
    {
        managedBatches.Values.ForEach(batchData => 
            batchData.octrees.ForEach(batch => batch.octreeBytes.Dispose())
        );
    }
}