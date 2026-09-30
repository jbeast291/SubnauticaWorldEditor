using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace SNTerrainEditor.Core.DataTypes;

internal unsafe partial class NativeGrid {
    
    /// <summary>Rasterizes an octree into the NativeGrid, overwriting it entirely</summary>
    internal void RasterizeOctreeIntoGrid(NativeArray<byte> octreeData) {
        if (!octreeData.IsCreated || octreeData.Length == 0) return;
        ConvertOctreeToGrid(octreeData.Reinterpret<OctNode>(), (ushort*)_gridPtr);
    }
    
    private static void ConvertOctreeToGrid(
        NativeArray<OctNode> nodes,
        ushort* gridPtr,
        int currentNodeIndex = 0, 
        uint currentNodeRegionWidth = SideLength,
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

    private const ushort MAX_NODES_OCTREE 
        = (32 * 32 * 32) + (16 * 16 * 16) + (8 * 8 * 8) + (4 * 4 * 4) + (2 * 2 * 2) + 1;
    
    private static readonly NativeArray<OctNode> buffer = new(MAX_NODES_OCTREE, Allocator.Persistent);
    private static readonly OctNode* bufferPtr = (OctNode*)buffer.GetUnsafePtr();
    private ushort bufferPos;
    
    /// <summary>
    /// Converts the native terrain grid into an octree byte sequence compatible with the terrain streamer.
    /// </summary>
    /// <remarks>If this is being written to a file, it does not include the node count ushort in the array,
    /// it is just the raw nodes in correct order</remarks>
    internal NativeArray<byte> DerasterizeToOctree() {
        bufferPos = (ushort)buffer.Length;

        OctNode root = GenerateNodes(0, 0, 0, 5);
        //root node is not assigned in loop
        bufferPos -= 1;
        bufferPtr[bufferPos] = root;
        
        OctNode GenerateNodes(uint x, uint y, uint z, int size) {
            if (size == 0) {
                Voxel voxel = _gridPtr[GetVoxelIndex(z, y, x)];
                return new OctNode()
                {
                    density = voxel.density,
                    type = voxel.type,
                    childIndex = 0
                };
            }

            uint halfWidth = 1u << (size - 1);
            
            OctNode* children = stackalloc OctNode[8];
            int newSize = size - 1;
            children[0] = GenerateNodes(x,           y,           z,           newSize);
            children[1] = GenerateNodes(x,           y,           z+halfWidth, newSize);
            children[2] = GenerateNodes(x,           y+halfWidth, z,           newSize);
            children[3] = GenerateNodes(x,           y+halfWidth, z+halfWidth, newSize);
            children[4] = GenerateNodes(x+halfWidth, y,           z,           newSize);
            children[5] = GenerateNodes(x+halfWidth, y,           z+halfWidth, newSize);
            children[6] = GenerateNodes(x+halfWidth, y+halfWidth, z,           newSize);
            children[7] = GenerateNodes(x+halfWidth, y+halfWidth, z+halfWidth, newSize);

            OctNode first = children[0];
            bool isUniform = true;
            bool hasChildren = false;
            
            for (int i = 1; i < 8; i++) {
                OctNode current = children[i];
                if (first.density != current.density || first.type != current.type) {
                    isUniform = false;
                    break;
                }
                if (current.childIndex != 0) {
                    hasChildren = true;
                    break;
                }
            }

            if (isUniform && !hasChildren) {
                bufferPos -= 8;
                UnsafeUtility.MemCpy(bufferPtr + bufferPos, children, 8 * sizeof(OctNode));
                return first with { childIndex = bufferPos };
            }

            ushort densitySum = 0;
            byte* typeDictionary = stackalloc byte[byte.MaxValue];
            for (int i = 0; i < 8; i++) {
                OctNode current = children[i];
                densitySum += (current.density == 0 && current.type != 0) ? (byte)252 : current.density;
                typeDictionary[current.type]++;
            }
            
            byte avgDensity = (byte)(densitySum / 8);
            byte dominantType = 0;

            if (avgDensity >= 126) {
                int maxCount = 0;
                for (int t = 1; t < byte.MaxValue; t++)
                {
                    if (typeDictionary[t] <= maxCount) continue;
                    maxCount = typeDictionary[t];
                    dominantType = (byte)t;
                }
            }
            
            bufferPos -= 8;
            UnsafeUtility.MemCpy(bufferPtr + bufferPos, children, 8 * sizeof(OctNode));
            
            return new OctNode() {
                density = avgDensity,
                type = dominantType,
                childIndex = bufferPos,
            };
        }
        int nodeCount = ushort.MaxValue - bufferPos;
        
        NativeArray<OctNode> outputNodes = new(nodeCount, Allocator.Persistent);
        OctNode* outputNodesPtr = (OctNode*)outputNodes.GetUnsafePtr();
        
        UnsafeUtility.MemCpy(outputNodesPtr, bufferPtr + bufferPos, nodeCount * sizeof(OctNode));
        
        for (int i = 0; i < nodeCount; i++) {
            ref OctNode node = ref outputNodesPtr[i];
            node.childIndex = (ushort)(node.childIndex - bufferPos);
        }

        return outputNodes.Reinterpret<byte>();
    }
        

    /*
    private NativeList<OctNode> ConvertGridToOctree() {
        NativeList<OctNode> nodes = new(65534, Allocator.Persistent);
        
        // Breath First as nodes must be listed top down from the tree
        Queue<(int nodeIdx, uint x, uint y, uint z, uint width)> queue = new(128);
        
        // root
        nodes.Add(new());
        queue.Enqueue((0, 0, 0, 0, SideLength));

        while (queue.Count > 0)
        {
            (int nodeIdx, uint x, uint y, uint z, uint width) = queue.Dequeue();

            // Sample the region to determine dominant type, average density, uniformity
            SampleRegion(
                x, y, z, width,
                out byte dominantType,
                out byte avgDensity,
                out bool isUniform);
                
            if (isUniform || width == 1)
            {
                ref OctNode octNode = ref nodes.ElementAt(nodeIdx);
                // leaf Node, child index must be set to 0
                octNode.type = dominantType;
                octNode.density = avgDensity;
                octNode.childIndex = 0;
                continue;
            }
            // Internal node reserve 8 consecutive child slots
            ushort firstChildIdx = (ushort)nodes.Length;
            nodes.ResizeUninitialized(nodes.Length + 8);

            // We can only get a reference to the parent node AFTER we add the children.
            // When nativeList reallocates its memory to expand, the ref to the struct can become invalid and cause undefined behavior
            ref OctNode parent = ref nodes.ElementAt(nodeIdx);
                
            // Modify parent node to point to children.
            parent.type = dominantType;
            parent.density = avgDensity;
            parent.childIndex = firstChildIdx;
                
            uint half = width / 2;
            
            queue.Enqueue((firstChildIdx,     x,        y,        z,        half));
            queue.Enqueue((firstChildIdx + 1, x,        y       , z + half, half));
            queue.Enqueue((firstChildIdx + 2, x,        y + half, z,        half));
            queue.Enqueue((firstChildIdx + 3, x,        y + half, z + half, half));
            queue.Enqueue((firstChildIdx + 4, x + half, y,        z,        half));
            queue.Enqueue((firstChildIdx + 5, x + half, y,        z + half, half));
            queue.Enqueue((firstChildIdx + 6, x + half, y + half, z,        half));
            queue.Enqueue((firstChildIdx + 7, x + half, y + half, z + half, half));
        }
        return nodes;
    }
    */
}

