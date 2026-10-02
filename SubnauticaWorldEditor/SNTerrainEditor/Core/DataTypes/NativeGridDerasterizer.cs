using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace SNTerrainEditor.Core.DataTypes;

internal static unsafe class NativeGridDerasterizer
{
    private const ushort LEVEL1_NODE_COUNT = (16 * 16 * 16);
    private const ushort LEVEL2_NODE_COUNT = (8 * 8 * 8);
    private const ushort LEVEL3_NODE_COUNT = (4 * 4 * 4);
    private const ushort LEVEL4_NODE_COUNT = (2 * 2 * 2);
    private const ushort LEVEL5_NODE_COUNT = 1;
    private const ushort MAX_NODES_OCTREE = NativeGrid.GridArrayLength + LEVEL1_NODE_COUNT + LEVEL2_NODE_COUNT + 
                                            LEVEL3_NODE_COUNT + LEVEL4_NODE_COUNT + LEVEL5_NODE_COUNT;

    private static readonly NativeArray<OctNode> buffer = new(MAX_NODES_OCTREE, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
    
    /// <summary>
    /// Schedules the octree derasterization job.
    /// </summary>
    public static JobHandle Schedule(NativeGrid grid, NativeList<byte> outputBytes, JobHandle dependency = default) {
        DerasterizeToOctreeJob job = new() {
            GridPtr = grid.GridPtr,
            bufferPtr = (OctNode*)buffer.GetUnsafePtr(),
            OutputBytes = outputBytes
        };
        
        return job.Schedule(dependency);
    }
    
    [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
    private struct DerasterizeToOctreeJob : IJob {
        [NativeDisableUnsafePtrRestriction] public Voxel* GridPtr;
        [NativeDisableUnsafePtrRestriction] public OctNode* bufferPtr;
        private ushort bufferPos;
        
        public NativeList<byte> OutputBytes;
        
        public void Execute() {
            bufferPos = MAX_NODES_OCTREE;
            
            // Ping pong buffering system. We only need the layers 1 below the current
            // it's safe to overwrite after and reuse
            OctNode* LayerBufferPing = stackalloc OctNode[LEVEL1_NODE_COUNT + LEVEL2_NODE_COUNT];
            OctNode* LayerBufferPong = LayerBufferPing + LEVEL1_NODE_COUNT;
            
            //Process Levels
            for (uint src = 0; src < NativeGrid.GridArrayLength; src += 8) {
                LayerBufferPing[src >> 3] = CreateParentNodeVoxel(GridPtr + src);
            }
            for (uint srcIdx = 0; srcIdx < LEVEL1_NODE_COUNT; srcIdx += 8) {
                LayerBufferPong[srcIdx >> 3] = CreateParentNode(LayerBufferPing + srcIdx);
            }
            for (uint srcIdx = 0; srcIdx < LEVEL2_NODE_COUNT; srcIdx += 8) {
                LayerBufferPing[srcIdx >> 3] = CreateParentNode(LayerBufferPong + srcIdx);
            }
            for (uint srcIdx = 0; srcIdx < LEVEL3_NODE_COUNT; srcIdx += 8) {
                LayerBufferPong[srcIdx >> 3] = CreateParentNode(LayerBufferPing + srcIdx);
            }
            
            OctNode root = CreateParentNode(LayerBufferPong);
            
            bufferPos -= 1;
            bufferPtr[bufferPos] = root;
            int nodeCount = MAX_NODES_OCTREE - bufferPos;

            // Adjust child indexes
            for (int i = bufferPos; i < MAX_NODES_OCTREE; i++) {
                ref OctNode node = ref bufferPtr[i];
                if (node.childIndex != 0) node.childIndex = (ushort)(node.childIndex - bufferPos);
            }

            // Write out
            int byteCount = nodeCount * sizeof(OctNode);
            OutputBytes.ResizeUninitialized(byteCount);
            byte* outputPtr = (byte*)OutputBytes.GetUnsafePtr();
            UnsafeUtility.MemCpy(outputPtr, bufferPtr + bufferPos, byteCount);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private OctNode CreateParentNodeVoxel(Voxel* children)
        {
            Voxel first = children[0];
            bool isUniform = children[1].density == first.density && children[1].type == first.type &&
                             children[2].density == first.density && children[2].type == first.type &&
                             children[3].density == first.density && children[3].type == first.type &&
                             children[4].density == first.density && children[4].type == first.type &&
                             children[5].density == first.density && children[5].type == first.type &&
                             children[6].density == first.density && children[6].type == first.type &&
                             children[7].density == first.density && children[7].type == first.type;
            
            if (isUniform) return new OctNode {
                density = first.density,
                type = first.type,
                childIndex = 0,
            };

            ushort densitySum = 0;
            for (int i = 0; i < 8; i++) {
                Voxel current = children[i];
                densitySum += (current.density == 0 && current.type != 0) ? (byte)252 : current.density;
            }

            byte avgDensity = (byte)(densitySum >> 3);
            byte dominantType = 0;

            if (avgDensity >= 126) dominantType = GetDominantType(children);

            bufferPos -= 8;
            for (int i = 0; i < 8; i++) {
                bufferPtr[bufferPos + i] = new OctNode {
                    density = children[i].density,
                    type = children[i].type,
                    childIndex = 0
                };
            }

            return new OctNode {
                density = avgDensity,
                type = dominantType,
                childIndex = bufferPos,
            };
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private OctNode CreateParentNode(OctNode* children) { 
            bool hasChildren = children[0].childIndex != 0 || children[1].childIndex != 0 ||
                               children[2].childIndex != 0 || children[3].childIndex != 0 ||
                               children[4].childIndex != 0 || children[5].childIndex != 0 ||
                               children[6].childIndex != 0 || children[7].childIndex != 0;
            
            if (!hasChildren) {
                OctNode first = children[0];
                bool isUniform = children[1].density == first.density && children[1].type == first.type &&
                                 children[2].density == first.density && children[2].type == first.type &&
                                 children[3].density == first.density && children[3].type == first.type &&
                                 children[4].density == first.density && children[4].type == first.type &&
                                 children[5].density == first.density && children[5].type == first.type &&
                                 children[6].density == first.density && children[6].type == first.type &&
                                 children[7].density == first.density && children[7].type == first.type;
                
                if (isUniform) return first with { childIndex = 0 };
            }

            ushort densitySum = 0;
            for (int i = 0; i < 8; i++) {
                OctNode current = children[i];
                densitySum += (current.density == 0 && current.type != 0) ? (byte)252 : current.density;
            }

            byte avgDensity = (byte)(densitySum >> 3);
            byte dominantType = 0;

            if (avgDensity >= 126) dominantType = GetDominantType(children);

            bufferPos -= 8;
            UnsafeUtility.MemCpy(bufferPtr + bufferPos, children, 8 * sizeof(OctNode));

            return new OctNode {
                density = avgDensity,
                type = dominantType,
                childIndex = bufferPos,
            };
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte GetDominantType(Voxel* children) {
            byte bestType = 0;
            int bestCount = 0;

            for (int i = 0; i < 8; i++) {
                byte type = children[i].type;
                if (type == 0) continue;

                int count = 1;

                for (int j = i + 1; j < 8; j++) {
                    count += children[j].type == type ? 1 : 0;
                }

                if (count > bestCount) {
                    bestCount = count;
                    bestType = type;
                    
                    if (bestCount >= 4) break;
                }
            }
            return bestType;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte GetDominantType(OctNode* children) {
            byte bestType = 0;
            int bestCount = 0;

            for (int i = 0; i < 8; i++) {
                byte type = children[i].type;
                if (type == 0) continue;

                int count = 1;

                for (int j = i + 1; j < 8; j++) {
                    count += children[j].type == type ? 1 : 0;
                }

                if (count > bestCount) {
                    bestCount = count;
                    bestType = type;
                    
                    if (bestCount >= 4) break;
                }
            }
            return bestType;
        }
    }
}