using System;
using System.Collections.Generic;
using System.Linq;
using SNTerrainEditor.Patches;
using WorldStreaming;

namespace SNTerrainEditor.Core.DataTypes;

/// <summary>
/// Holds the currently editable voxel terrain for the active save.
/// </summary>
public class EditSession : IDisposable, IBatchStreamingEventListener
{
    public const int BatchSideLength = 160;
    public const int GridsPerBatch = 5;
    
    private readonly WorldStreamer _worldStreamer;
    
    private readonly Dictionary<Int3, NativeTerrainGrid> _editableGrids = new();
    private readonly HashSet<Int3> _managedBatchIndexes = new();

    public EditSession(WorldStreamer worldStreamer)
    {
        _worldStreamer = worldStreamer;
        BatchOctreesPatcher.RegisterListener(this);
    }
    
    public void AddBatch(Int3 batchIndex)
    {
        _managedBatchIndexes.Add(batchIndex);
        
        foreach (Int3 gridGlobalIndex in VoxelEditorUtils.IterateGridIndexesInBatch(batchIndex))
        {
            _editableGrids.Add(gridGlobalIndex, new(gridGlobalIndex));
        }

        BatchOctrees batchOctrees = _worldStreamer.octreesStreamer.batches.FirstOrDefault(batch => batch.id == batchIndex);
        if(batchOctrees != null) RegisterActiveBatch(batchOctrees);
    }

    private void RegisterActiveBatch(BatchOctrees batch)
    {
        Int3 batchGridOffset = batch.id * GridsPerBatch;
        
        foreach (Int3 gridLocalIndex in VoxelEditorUtils.IterateLocalGridIndexes())
        {
            Octree octree = batch.GetOctree(gridLocalIndex);

            NativeTerrainGrid terrainGrid = _editableGrids[batchGridOffset + gridLocalIndex];
            terrainGrid.SetGridsByOctree(octree);
            //give the array back to the allocator, so we can manage the array ourselves without starving the pool
            octree.Clear(BatchOctreesAllocator.octreePool);
            terrainGrid.UpdateAssociatedOctree();
        }
    }
    
    void IBatchStreamingEventListener.OnBatchLoaded(BatchOctrees batchOctrees)
    {
        if (!_managedBatchIndexes.Contains(batchOctrees.id)) return;
        RegisterActiveBatch(batchOctrees);
    }
    void IBatchStreamingEventListener.OnBatchUnloaded(BatchOctrees batchOctrees)
    {
        if (!_managedBatchIndexes.Contains(batchOctrees.id)) return;
        foreach (Int3 gridGlobalIndex in VoxelEditorUtils.IterateGridIndexesInBatch(batchOctrees.id))
        {
            _editableGrids[gridGlobalIndex].UnloadGrid();
        }
    }
    void IBatchStreamingEventListener.OnEarlyClearOctrees(BatchOctrees batchOctrees)
    {
        if (!_managedBatchIndexes.Contains(batchOctrees.id)) return;
        
        //TODO: when a managed batch is unloaded, we need to cache to disk the state of the batch.
        //  Some kind of cache location will be needed. Prob need a save system for this to work properly.
        foreach (Octree octree in batchOctrees.octrees)
        {
            octree.data.Dispose();
        }
    }

    public void DEBUG__ModifyAllLava()
    {
        foreach (NativeTerrainGrid terrainGrid in _editableGrids.Values)
        {
            terrainGrid.DEBUG__ModifyWithLavaTexture();
            terrainGrid.UpdateAssociatedOctree();
        }
    }

    public void EndSession()
    {
        BatchOctreesPatcher.DeregisterListener(this);
        Dispose();
    }
    
    public void Dispose()
    {
        _editableGrids.Values.ForEach(terrainGrid => terrainGrid.Dispose());
    }
}