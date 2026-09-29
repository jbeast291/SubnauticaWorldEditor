using System;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;

namespace SNTerrainEditor.Core.DataTypes;

internal partial class NativeGrid
{
    private unsafe void SampleRegion(uint x, uint y, uint z, uint regionWidth,
        out byte dominantType,
        out byte avgDensity,
        out bool isUniform
    ) {
        uint startPos = GetVoxelIndex(z, y, x);
        uint regionVolume = regionWidth * regionWidth * regionWidth;
        Voxel* ptr = _gridPtr + startPos;

        Voxel firstVoxel = ptr[0];
        isUniform = true;
        if (regionVolume <= 1) 
        {
            isUniform = true;
        }
        else
        {
            int voxelSize = UnsafeUtility.SizeOf<Voxel>();
            long totalBytesToCompare = (regionVolume - 1) * voxelSize;
            // compare the array to itself but the elements shifted 1 over
            isUniform = UnsafeUtility.MemCmp(ptr, ptr + 1, totalBytesToCompare) == 0;
        }
        
        // if the region is already uniform, no reason to check every value
        if (isUniform)
        {
            byte finalDensity = GetAdjustedDensity(firstVoxel);
            avgDensity = finalDensity;
            dominantType = (avgDensity >= 126) ? firstVoxel.type : (byte)0;
            return;
        }

        long densitySum = 0;
        Span<int> typeDictionary = stackalloc int[byte.MaxValue];
        for (uint i = 0; i < regionVolume; i++) {
            Voxel voxel = ptr[i];
            densitySum += GetAdjustedDensity(voxel);;
            typeDictionary[voxel.type]++;
        }

        // ReSharper disable once IntDivisionByZero - its not possible for a region volume to be 0 atp
        avgDensity = (byte)(densitySum / regionVolume);
        dominantType = 0;

        if (avgDensity < 126) return;

        int maxCount = 0;
        for (int t = 1; t < byte.MaxValue; t++)
        {
            if (typeDictionary[t] <= maxCount) continue;
            maxCount = typeDictionary[t];
            dominantType = (byte)t;
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte GetAdjustedDensity(Voxel voxel)
    {
        return (voxel.density == 0 && voxel.type != 0) ? (byte)252 : voxel.density;
    }
}