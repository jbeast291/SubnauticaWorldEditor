using System.Collections;
using SNTerrainEditor.Core.DataTypes;
using SNTerrainEditor.Extensions;
using UnityEngine;
using WorldStreaming;
using Math = System.Math;

namespace SNTerrainEditor.Core;

public class EditorBatchManager : MonoBehaviour
{
    public static EditorBatchManager main { get; private set;}
    
    private WorldStreamer worldStreamer;
    
    public EditSession activeSession { get; private set; }
    
    public void Awake()
    {
        if (main != null)
        {
            Plugin.Logger.LogError("Duplicate Editor Batch Manager found!");
            DestroyImmediate(this);
            return;
        }
        main = this;
    }

    public IEnumerator Start()
    {
        yield return new WaitUntil( () => LargeWorldStreamer.main.streamerV2 != null);
        worldStreamer = LargeWorldStreamer.main.streamerV2;

        activeSession = new(worldStreamer);
    }

    //TEMPORARY UTILITY FOR UNITY EXPLORER
    public Int3 CreateInt3(int x, int y, int z)
    {
        return new(x, y, z);
    }

    public ClipmapCell GetActiveCellForPosition(Int3 position)
    {
        Int3.Bounds blockBounds = new(position, position);

        ClipmapLevel level = LargeWorldStreamer.main.streamerV2.clipmapStreamer.levels[0];

        foreach (Int3 cellID in level.GetCellRange(blockBounds))
        {
            return level.GetCell(cellID);  
        }

        return null;
    }
    
    public void DEBUG__Batch121812Lava()
    {
                
        System.Diagnostics.Stopwatch sw = new();
        sw.Start();
        try { activeSession.AddBatch(new(12, 18, 12)); } catch {}
        sw.Stop();
        Plugin.Logger.LogError($"TOOK {sw.ElapsedMilliseconds}ms");

        sw.Restart();
        activeSession.DEBUG__ModifyAllLava();
        sw.Stop();
        Plugin.Logger.LogError($"TOOK {sw.ElapsedMilliseconds}ms");
        
        DEBUG__RefreshMeshForBatch(12, 18, 12);
    }
    
    public void DEBUG__ClearBatchOctrees(int x, int y, int z)
    {
        BatchOctreesStreamer octreesStreamer = worldStreamer.octreesStreamer;
        octreesStreamer.GetBatch(new Int3(x, y, z))?.ClearOctrees();
    }
    
    public void DEBUG__RefreshMeshForBatch(int x, int y, int z)
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