using System;
using System.Diagnostics;
using SNTerrainEditor.Core.DataTypes;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using WorldStreaming;

namespace SNTerrainEditor.Extensions;

internal static unsafe class OctreeRasterizer {
    /// <summary>Rasterize an octree to a 32x32x32 grid of points</summary>
    internal static void RasterizeToGrid(NativeArray<byte> octreeData, Voxel* grid) {
        if (!octreeData.IsCreated || octreeData.Length == 0) return;
        ConvertOctreeToGrid(octreeData.Reinterpret<OctNode>(), (ushort*)grid);
    }
    
    private static void ConvertOctreeToGrid(
        NativeArray<OctNode> nodes,
        ushort* gridPtr,
        int currentNodeIndex = 0, 
        uint currentNodeRegionWidth = NativeGrid.SideLength,
        uint x = 0, uint y = 0, uint z = 0
    ) {
        OctNode currentNode = nodes[currentNodeIndex];
        ushort startingChildIndex = currentNode.childIndex;
        
        // Leaf Node, safe to fill in the grid from here
        if (startingChildIndex == 0 || currentNodeRegionWidth <= 1) {
            int startIndex = (int)NativeGrid.GetVoxelIndex(z, y, x);
            ushort voxelPacked = (ushort)(currentNode.type | (currentNode.density << 8));
            int volume = (int)(currentNodeRegionWidth * currentNodeRegionWidth * currentNodeRegionWidth);
            
            ushort* dst = gridPtr + startIndex;
            dst[0] = voxelPacked;
            
            // Gradually copy the ushort for the region we want. This is the only real way to (blazingly fast)
            // fill a ushort value for a continuous region
            int filled = 1;
            while (filled < volume) {
                int countToCopy = Math.Min(filled, volume - filled);
                UnsafeUtility.MemCpy(dst + filled, dst, countToCopy * sizeof(ushort));
                filled += countToCopy;
            }
            return;
        }

        // Subdivide work into child nodes
        uint half = currentNodeRegionWidth / 2;
        ConvertOctreeToGrid(nodes, gridPtr, startingChildIndex,     half, x,        y,        z       );
        ConvertOctreeToGrid(nodes, gridPtr, startingChildIndex + 1, half, x,        y,        z + half);
        ConvertOctreeToGrid(nodes, gridPtr, startingChildIndex + 2, half, x,        y + half, z       );
        ConvertOctreeToGrid(nodes, gridPtr, startingChildIndex + 3, half, x,        y + half, z + half);
        ConvertOctreeToGrid(nodes, gridPtr, startingChildIndex + 4, half, x + half, y,               z);
        ConvertOctreeToGrid(nodes, gridPtr, startingChildIndex + 5, half, x + half, y,        z + half);
        ConvertOctreeToGrid(nodes, gridPtr, startingChildIndex + 6, half, x + half, y + half, z       );
        ConvertOctreeToGrid(nodes, gridPtr, startingChildIndex + 7, half, x + half, y + half, z + half);
    }
}