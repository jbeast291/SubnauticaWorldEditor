using System.Collections;
using SNTerrainEditor;
using TerrainEditor.Core.DataTypes;
using TerrainEditor.Extensions;
using UnityEngine;
using WorldStreaming;
using Math = System.Math;

namespace TerrainEditor.Core;

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
    
    //TEMPORARY UTILITY FOR UNITY EXPLORER
    public Int3.Bounds CreateBounds(int x1, int y1, int z1, int x2, int y2, int z2)
    {
        return new(new(x1, y1, z1), new(x2, y2, z2));
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