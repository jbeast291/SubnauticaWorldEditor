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
        NativeList<OctNode> nodes = new(1024, Allocator.Persistent);
        
        // Breath First as nodes must be listed top down from the tree
        Queue<(int nodeIdx, uint x, uint y, uint z, uint width)> queue = new(128);
        
        // root
        nodes.Add(new());
        queue.Enqueue((0, 0, 0, 0, NativeTerrainGrid.SideLength));

        while (queue.Count > 0)
        {
            (int nodeIdx, uint x, uint y, uint z, uint width) = queue.Dequeue();

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
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct OctNode
{
    public byte type;
    public byte density;
    public ushort childIndex;
}