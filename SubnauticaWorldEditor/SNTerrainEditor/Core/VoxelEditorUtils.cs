using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TerrainEditor.Core.DataTypes;

namespace TerrainEditor.Core;

public static class VoxelEditorUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<Int3> IterateLocalGridIndexes()
    {
        for(int x = 0; x < EditSession.GridsPerBatch; x++)
        for(int y = 0; y < EditSession.GridsPerBatch; y++)
        for(int z = 0; z < EditSession.GridsPerBatch; z++)
        {
            yield return new(x, y, z);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<Int3> IterateGridIndexesInBatch(Int3 batchIndex)
    {
        Int3 batchGridOffset = batchIndex * EditSession.GridsPerBatch;
        foreach (Int3 gridLocalIndex in IterateLocalGridIndexes())
        {
            yield return batchGridOffset + gridLocalIndex;
        }
    }
}