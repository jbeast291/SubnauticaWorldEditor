using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace SNTerrainEditor.Core.DataTypes;

internal static unsafe class NativeGridRasiterizer {
    
    /// <summary>Rasterizes an octree into the NativeGrid, overwriting it entirely</summary>
    internal static void FromOctreeIntoGrid(NativeGrid grid, NativeArray<byte> octreeData) {
        if (!octreeData.IsCreated || octreeData.Length == 0) return;
        ConvertOctreeToGrid(octreeData.Reinterpret<OctNode>(), (ushort*)grid.GridPtr);
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
            ushort* startVoxel = gridPtr + startIndex;
            
            if (currentNodeRegionWidth == 1) {
                *startVoxel = voxelPacked;
                return;
            }
            
            int volume = (int)(currentNodeRegionWidth * currentNodeRegionWidth * currentNodeRegionWidth);
            
            UnsafeUtility.MemCpyReplicate(
                destination: startVoxel, 
                source: &voxelPacked, 
                size: sizeof(ushort), 
                count: volume
            );
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
