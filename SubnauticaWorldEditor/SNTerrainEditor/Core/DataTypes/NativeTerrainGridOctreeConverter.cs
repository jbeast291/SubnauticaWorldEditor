using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace SNTerrainEditor.Core.DataTypes;

public static class NativeTerrainGridOctreeConverter
{
    extension(NativeTerrainGrid terrainGrid)
    {
        /// <summary>
        /// Converts the native terrain grid into an octree byte sequence compatible with the terrain streamer.
        /// </summary>
        /// <remarks>If this is being written to a file, it does not include the node count ushort in the array,
        /// it is just the raw nodes in correct order</remarks>
        public NativeArray<byte> GetAsOctreeBytes()
        {
            return ConvertGridToOctree(terrainGrid)
                    .AsArray()
                    .Reinterpret<byte>(UnsafeUtility.SizeOf<OctNode>());
        }
    }

    private static NativeList<OctNode> ConvertGridToOctree(NativeTerrainGrid terrainGrid)
    {
        // Each node is 4 bytes as a struct.
        // We can modify the nodes afterward.
        NativeList<OctNode> nodes = new(Allocator.Persistent);

        // Queue allow checking parent nodes before adding child nodes
        // Breath First in a sense as nodes must be listed top down from the tree
        Queue<(int nodeIdx, int x, int y, int z, int width)> queue = new();

        //Add root node
        nodes.Add(new());
        //Enqueue it to start off the "recursive" sequence
        queue.Enqueue((0, 0, 0, 0, NativeTerrainGrid.SideLength));//start at the entire size of the grid

        while (queue.Count > 0)
        {
            (int nodeIdx, int x, int y, int z, int width) = queue.Dequeue();

            // Sample the region to determine dominant type, average density, uniformity
            terrainGrid.SampleRegion(
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
            }
            else
            {
                // Internal node — reserve 8 consecutive child slots
                ushort firstChildIdx = (ushort)nodes.Length;
                for (int c = 0; c < 8; c++)
                    nodes.Add(new());

                //We can only get a reference to the parent node AFTER we add the children.
                //When nativeList reallocates its memory to expand, the ref to the struct can become invalid and cause undefined behavior
                ref OctNode octNode = ref nodes.ElementAt(nodeIdx);
                    
                // Modify parent node to point to children.
                octNode.type = dominantType;
                octNode.density = avgDensity;
                octNode.childIndex = firstChildIdx;

                // Enqueue 8 children 
                int half = width / 2;
                for (int i = 0; i < 8; i++)
                {
                    Int3 offset = CornerOffsets[i] * half;
                    queue.Enqueue((firstChildIdx + i, x + offset.x, y + offset.y, z + offset.z, half));
                }
            }
        }
        return nodes;
    }
    
    //TODO: check if hitting this is slower than generating the offsets from a byte sequence
    private static readonly Int3[] CornerOffsets =
    [
        new (0, 0, 0),
        new (0, 0, 1),
        new (0, 1, 0),
        new (0, 1, 1),
        new (1, 0, 0),
        new (1, 0, 1),
        new (1, 1, 0),
        new (1, 1, 1)
    ];
        
    //ensure this struct is stored in memory like an array of bytes
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct OctNode
    {
        public byte type;
        public byte density;
        public ushort childIndex;
    }
}