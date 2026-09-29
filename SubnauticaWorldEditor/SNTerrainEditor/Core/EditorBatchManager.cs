using System.Collections;
using SNTerrainEditor.Core.DataTypes;
using SNTerrainEditor.Extensions;
using SNTerrainEditor.FileOperations;
using Unity.Collections;
using UnityEngine;
using WorldStreaming;
using Math = System.Math;

namespace SNTerrainEditor.Core;

internal class EditorBatchManager : MonoBehaviour
{
    internal static EditorBatchManager main { get; private set;}
    
    private WorldStreamer worldStreamer;
    
    internal EditSession activeSession { get; private set; }
    
    private void Awake()
    {
        if (main != null)
        {
            Plugin.LogError("Duplicate Editor Batch Manager found!");
            DestroyImmediate(this);
            return;
        }
        main = this;
    }

    private IEnumerator Start()
    {
        yield return new WaitUntil( () => LargeWorldStreamer.main.streamerV2 != null);
        worldStreamer = LargeWorldStreamer.main.streamerV2;

        activeSession = new(worldStreamer);
    }
    
    private Int3 CreateInt3(int x, int y, int z)
    {
        return new(x, y, z);
    }

    private ClipmapCell GetActiveCellForPosition(Int3 position)
    {
        Int3.Bounds blockBounds = new(position, position);

        ClipmapLevel level = LargeWorldStreamer.main.streamerV2.clipmapStreamer.levels[0];

        foreach (Int3 cellID in level.GetCellRange(blockBounds))
        {
            return level.GetCell(cellID);  
        }

        return null;
    }
    
    public void DEBUG__Batch121812Modify()
    {
        System.Diagnostics.Stopwatch sw = new();
        sw.Start();
        activeSession.AddBatch(new(12, 18, 12));
        sw.Stop();
        Plugin.LogError($"Reading + Octree Allocations Took {sw.ElapsedMilliseconds}ms");
        
        sw.Restart();
        activeSession.DEBUG__Clear();
        sw.Stop();
        Plugin.LogError($"CLEAR (Rasterize + Modify + Derasterize) Took: {sw.ElapsedMilliseconds}ms");
        
        sw.Restart();
        activeSession.DEBUG__Sphere(new(12, 18, 12));
        sw.Stop();
        Plugin.LogError($"Create Spheres (Rasterize + Modify + Derasterize) Took: {sw.ElapsedMilliseconds}ms");
        
        sw.Restart();
        DEBUG__UploadChangesToWorldStreamer(new(12, 18, 12));
        sw.Stop();
        Plugin.LogError($"Upload Took: {sw.ElapsedMilliseconds}ms");
        
        sw.Restart();
        DEBUG__RefreshMeshForBatch(12, 18, 12);
        sw.Stop(); 
        Plugin.LogError($"Visual Refreshed Queued In: {sw.ElapsedMilliseconds}ms");
    }

    private void DEBUG__UploadChangesToWorldStreamer(Int3 batchID) {
        BatchOctreesStreamer octreesStreamer = worldStreamer.octreesStreamer;
        BatchOctrees batchOctrees = octreesStreamer.GetBatch(batchID);
        
        foreach (Int3 octreeLocalIndex in Int3.Range(ManagedBatch.OCTREES_PER_SIDE)) {
            Octree octree = batchOctrees.octrees.Get(octreeLocalIndex);
            batchOctrees.allocator.Return(octree.data);
            octree.data = activeSession.GetBatchOctree(batchID, octreeLocalIndex).octreeBytes;
        }
    }
    
    private void DEBUG__RefreshMeshForBatch(int x, int y, int z)
    {
        const int batchSize = 160;
        Int3 batchMinBlockPos = new(x * batchSize, y * batchSize, z * batchSize);

        BatchOctreesStreamer octreesStreamer = worldStreamer.octreesStreamer;
        BatchOctreesStreamer lowDetailOctreesStreamer = worldStreamer.lowDetailOctreesStreamer;
        
        int minLevel = Math.Min(octreesStreamer.minLod, lowDetailOctreesStreamer.minLod);
        int maxLevel = Math.Max(octreesStreamer.maxLod, lowDetailOctreesStreamer.maxLod);
        
        Int3.Bounds batchBlockRange = new(batchMinBlockPos, batchMinBlockPos + (Int3.one * 160));
        
        ClipmapLevel[] levels = worldStreamer.clipmapStreamer.levels;
        
        for (int i = minLevel; i < maxLevel; i++)
        {
            ClipmapLevel level = levels[i];
            foreach (Int3 cellIDForLevel in level.GetCellRange(batchBlockRange))
            {
                level.GetCell(cellIDForLevel)?.RefreshMeshAsync();
            }
        }
    }
}