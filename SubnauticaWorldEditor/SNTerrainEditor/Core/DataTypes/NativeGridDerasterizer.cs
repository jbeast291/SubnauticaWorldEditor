using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace SNTerrainEditor.Core.DataTypes;

[BurstCompile]
internal static unsafe class NativeGridDerasterizer
{
    private const ushort MAX_NODES_OCTREE 
        = (32 * 32 * 32) + (16 * 16 * 16) + (8 * 8 * 8) + (4 * 4 * 4) + (2 * 2 * 2) + 1;
    
    private static readonly NativeArray<OctNode> buffer = new(MAX_NODES_OCTREE, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
    private static readonly OctNode* bufferPtr = (OctNode*)buffer.GetUnsafePtr();
    private static ushort bufferPos;
    
    /// <summary>
    /// Converts the native terrain grid into an octree byte sequence compatible with the terrain streamer.
    /// </summary>
    /// <remarks>If this is being written to a file, it does not include the node count ushort in the array,
    /// it is just the raw nodes in correct order</remarks>
    internal static NativeArray<byte> ToOctree(NativeGrid grid) {
        bufferPos = (ushort)buffer.Length;

        OctNode root = GenerateNodes(grid.GridPtr, 0, 0, 0, 5);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static OctNode GenerateNodes(Voxel* gridPtr, uint x, uint y, uint z, int size) {
            uint halfWidth = 1u << (size - 1);
            
            OctNode* children = stackalloc OctNode[8];
            if (size == 1) {
                // Since nodes are stored in a Z order curve, we can read sequentially
                uint voxelIndex = NativeGrid.GetVoxelIndex(z, y, x);
                for (int i = 0; i < 8; i++) {
                    ref OctNode child = ref children[i];
                    Voxel voxel = gridPtr[voxelIndex + i];
                    child.type = voxel.type;
                    child.density = voxel.density;
                    child.childIndex = 0;
                }
            }
            else {
                int newSize = size - 1;
                children[0] = GenerateNodes(gridPtr, x,           y,           z,           newSize);
                children[1] = GenerateNodes(gridPtr, x,           y,           z+halfWidth, newSize);
                children[2] = GenerateNodes(gridPtr, x,           y+halfWidth, z,           newSize);
                children[3] = GenerateNodes(gridPtr, x,           y+halfWidth, z+halfWidth, newSize);
                children[4] = GenerateNodes(gridPtr, x+halfWidth, y,           z,           newSize);
                children[5] = GenerateNodes(gridPtr, x+halfWidth, y,           z+halfWidth, newSize);
                children[6] = GenerateNodes(gridPtr, x+halfWidth, y+halfWidth, z,           newSize);
                children[7] = GenerateNodes(gridPtr, x+halfWidth, y+halfWidth, z+halfWidth, newSize);
            }


            bool hasChildren;
            if (size == 1) {
                hasChildren = false;
            } else {
                hasChildren = children[0].childIndex != 0 || children[1].childIndex != 0 ||
                              children[2].childIndex != 0 || children[3].childIndex != 0 ||
                              children[4].childIndex != 0 || children[5].childIndex != 0 ||
                              children[6].childIndex != 0 || children[7].childIndex != 0;
            }
            
            OctNode first = children[0];
            bool isUniform = children[1].density == first.density && children[1].type == first.type &&
                             children[2].density == first.density && children[2].type == first.type &&
                             children[3].density == first.density && children[3].type == first.type &&
                             children[4].density == first.density && children[4].type == first.type &&
                             children[5].density == first.density && children[5].type == first.type &&
                             children[6].density == first.density && children[6].type == first.type &&
                             children[7].density == first.density && children[7].type == first.type;

            if (isUniform && !hasChildren) return first with { childIndex = 0 };

            ushort densitySum = 0;
            for (int i = 0; i < 8; i++) {
                OctNode current = children[i];
                densitySum += (current.density == 0 && current.type != 0) ? (byte)252 : current.density;
            }
            
            byte avgDensity = (byte)(densitySum >> 3);
            byte dominantType = 0;

            if (avgDensity >= 126) {
                dominantType = GetDominantType(children);
            }
            
            bufferPos -= 8;
            UnsafeUtility.MemCpy(bufferPtr + bufferPos, children, 8 * sizeof(OctNode));
            
            return new OctNode() {
                density = avgDensity,
                type = dominantType,
                childIndex = bufferPos,
            };
        }
        // add root to buffer
        bufferPos -= 1;
        bufferPtr[bufferPos] = root;
        
        int nodeCount = MAX_NODES_OCTREE - bufferPos;
        
        NativeArray<OctNode> outputNodes = new(nodeCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        OctNode* outputNodesPtr = (OctNode*)outputNodes.GetUnsafePtr();
        
        UnsafeUtility.MemCpy(outputNodesPtr, bufferPtr + bufferPos, nodeCount * sizeof(OctNode));
        
        for (int i = 0; i < nodeCount; i++) {
            ref OctNode node = ref outputNodesPtr[i];
            if (node.childIndex != 0) node.childIndex = (ushort)(node.childIndex - bufferPos);
        }
        
        return outputNodes.Reinterpret<byte>();
    }
    
    private static byte GetDominantType(OctNode* children)
    {
        byte maxType = 0;
        int maxCount = 0;

        for (int i = 0; i < 8; i++) {
            byte type = children[i].type;
            if (type == 0) continue;

            int count = 1;
            for (int j = i + 1; j < 8; j++) {
                if (children[j].type == type) count++;
            }
            if (count > maxCount) {
                maxCount = count;
                maxType = type;
            }
        }
        return maxType;
    } 
}