using SNTerrainEditor.Core.DataTypes;
using Unity.Collections;
using WorldStreaming;

namespace SNTerrainEditor.Extensions;

public static class OctreeRasterizer
{
    extension(Octree octree)
    {
        public void RasterizeToGrid(out NativeArray<byte> densityGrid, out NativeArray<byte> typeGrid)
        {
            densityGrid = new(NativeTerrainGrid.GridArrayLength, Allocator.Persistent);
            typeGrid = new(NativeTerrainGrid.GridArrayLength, Allocator.Persistent);

            ConvertOctreeToGrid(octree.data, densityGrid, typeGrid);
        }
    }

    /// <summary>
    /// Recursively converts a singular octree from an optoctree file into a dense 3d grid
    /// The densityGrid and typeGrid you supply will be filled out and be of the correct size
    /// </summary>
    private static void ConvertOctreeToGrid(
        NativeArray<byte> data,
        NativeArray<byte> densityGrid,
        NativeArray<byte> typeGrid,
        int currentNodeIndex = 0, 
        int currentNodeRegionWidth = NativeTerrainGrid.SideLength,
        int x = 0, int y = 0, int z = 0)
    {
        int nodeByteOffset = currentNodeIndex * 4;
            
        byte type = data[nodeByteOffset];
        byte density = data[nodeByteOffset + 1];
        ushort startingChildIndex = (ushort)(data[nodeByteOffset + 2] | (data[nodeByteOffset + 3] << 8));
            
        // Leaf Node, safe to fill in the grid from here
        if (startingChildIndex == 0)
        {
            for (int ix = x; ix < x + currentNodeRegionWidth; ix++)
            for (int iy = y; iy < y + currentNodeRegionWidth; iy++)
            for (int iz = z; iz < z + currentNodeRegionWidth; iz++)
            {
                int pos = NativeTerrainGrid.GetBlockIndex(ix, iy, iz);
                densityGrid[pos] = density;
                typeGrid[pos] = type;
            }
            return;
        }
            
        // Region size per node is halved, the area one covers decreases by a factor of 8 but the perimeter only by 2
        // In other words the side length of the region this node represents is half when subdividing
        int half = currentNodeRegionWidth / 2; 
            
        /*
           0	000
           1	001
           2	010
           3	011
           4	100
           5	101
           6	110
           7	111
         */
        for (byte i = 0; i < 8; i++)
        {
            int childIndex = startingChildIndex + i;
                
            //bitwise-and operations. Explanations simplify to 3 bits instead of the entire int but the idea is there.
            int ox = (i & 4) != 0 ? half : 0;//looks for 3rd bit, determines left or right
            //for example if i = 1, in binary it's represented as 001. 4 is 100. So the resulting bitwise-and of the two is 000 which = 0 and thus the offset is set to 0
            //Alternatively if i = 5, in binary it's 101, so when bitwise-and with 100 or 4, the result is 100. This does not equal 0 so the iteration is shifted over half.
            int oy = (i & 2) != 0 ? half : 0;//looks for 2nd bit, determines bottom or top
            int oz = (i & 1) != 0 ? half : 0;//looks for first bit, determines front or back

            ConvertOctreeToGrid(data, densityGrid, typeGrid, childIndex, half, x + ox, y + oy, z + oz);
        }
    }
}