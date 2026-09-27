using System;
using SNTerrainEditor.Core.DataTypes;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using WorldStreaming;

namespace SNTerrainEditor.Extensions;

public static unsafe class OctreeRasterizer
{
    extension(Octree octree)
    {
        public NativeArray<Voxel> RasterizeToGrid()
        {
            NativeArray<Voxel> grid = new(NativeTerrainGrid.GridArrayLength, Allocator.Persistent);
            if (!octree.data.IsCreated || octree.data.Length == 0) return grid;
            unsafe { ConvertOctreeToGrid((OctNode*)octree.data.GetUnsafePtr(), (ushort*)grid.GetUnsafePtr()); }
            return grid;
        }
    }
    
    private static unsafe void ConvertOctreeToGrid(
        OctNode* nodes,
        ushort* grid,
        int currentNodeIndex = 0, 
        int currentNodeRegionWidth = NativeTerrainGrid.SideLength,
        int x = 0, int y = 0, int z = 0
    ) {
        OctNode* currentNode = nodes + currentNodeIndex;
        ushort startingChildIndex = currentNode->childIndex;
        
        // Leaf Node, safe to fill in the grid from here
        if (startingChildIndex == 0 || currentNodeRegionWidth <= 1) {
            ushort voxelPacked = (ushort)(currentNode->type | (currentNode->density << 8));
            
            for (int iz = z; iz < z + currentNodeRegionWidth; iz++){
                int zOffset = iz * NativeTerrainGrid.SideLength * NativeTerrainGrid.SideLength;
                for (int iy = y; iy < y + currentNodeRegionWidth; iy++) {
                    int startPos = x + (iy * NativeTerrainGrid.SideLength) + zOffset;
                    new Span<ushort>(grid + startPos, currentNodeRegionWidth).Fill(voxelPacked);
                }
            }
            return;
        }

        // Subdivide work into child nodes
        int half = currentNodeRegionWidth / 2;
        ConvertOctreeToGrid(nodes, grid, startingChildIndex,     half, x,        y,        z       );
        ConvertOctreeToGrid(nodes, grid, startingChildIndex + 1, half, x,        y,        z + half);
        ConvertOctreeToGrid(nodes, grid, startingChildIndex + 2, half, x,        y + half, z       );
        ConvertOctreeToGrid(nodes, grid, startingChildIndex + 3, half, x,        y + half, z + half);
        ConvertOctreeToGrid(nodes, grid, startingChildIndex + 4, half, x + half, y,               z);
        ConvertOctreeToGrid(nodes, grid, startingChildIndex + 5, half, x + half, y,        z + half);
        ConvertOctreeToGrid(nodes, grid, startingChildIndex + 6, half, x + half, y + half, z       );
        ConvertOctreeToGrid(nodes, grid, startingChildIndex + 7, half, x + half, y + half, z + half);
    }
}