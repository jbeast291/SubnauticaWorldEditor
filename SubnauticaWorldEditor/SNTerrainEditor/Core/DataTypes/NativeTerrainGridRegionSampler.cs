using System;

namespace SNTerrainEditor.Core.DataTypes;

public partial class NativeTerrainGrid
{
    public unsafe void SampleRegion(int x, int y, int z, int regionWidth, 
        out byte dominantType,
        out byte avgDensity,
        out bool isUniform
    ) {
        long densitySum = 0;
        int sampleCount = 0;
            
        // Allocate it to the stack for efficiently reasons. A dictionary is too slow but 256 bytes fits easily on the stack
        Span<int> typeDictionary = stackalloc int[254];//255 is reserved and won't ever show up so we can ignore it

        byte firstType = 0;
        byte firstDensity = 0;
        bool first = true;
        isUniform = true;

        for (int ix = x; ix < x + regionWidth; ix++)
        for (int iy = y; iy < y + regionWidth; iy++)
        for (int iz = z; iz < z + regionWidth; iz++)
        {
            int pos = GetVoxelIndex(ix, iy, iz);
            Voxel voxel = _gridPtr[pos];
            byte density = voxel.density;
            byte type = voxel.type; 
                
            // special case, when the type not 0 but the density is 0 treat it as 252
            if (density == 0 && type != 0) density = 252;
                
            typeDictionary[type]++;
            densitySum += density;
            sampleCount++;

            if (first)
            {
                firstType = type;
                firstDensity = density;
                first = false;
            }
            else if (isUniform && (type != firstType || density != firstDensity))
            {
                isUniform = false;
            }
        }
            
        avgDensity = sampleCount > 0 ? (byte)(densitySum / sampleCount) : (byte)0;
        dominantType = 0;
            
        if (avgDensity < 126) return;
        //determine the best possible material that is non-air
        int maxCount = 0;
        //start iterating at 1 as the terrain is solid
        for (int t = 1; t < 254; t++)
        {
            if (typeDictionary[t] <= maxCount) continue;
                
            maxCount = typeDictionary[t];
            dominantType = (byte)t;
        }
    }
}