using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace SNTerrainEditor.Core.DataTypes;

internal static unsafe class NativeGridDerasterizer
{
    private const ushort MAX_NODES_OCTREE 
        = (32 * 32 * 32) + (16 * 16 * 16) + (8 * 8 * 8) + (4 * 4 * 4) + (2 * 2 * 2) + 1;

    private static readonly NativeArray<OctNode> buffer = new(MAX_NODES_OCTREE, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
    
    
    /// <summary>
    /// Schedules the octree derasterization job.
    /// </summary>
    public static JobHandle Schedule(NativeGrid grid, NativeList<byte> outputBytes, JobHandle dependency = default)
    {
        DerasterizeToOctreeJob job = new() {
            GridPtr = grid.GridPtr,
            bufferPtr = (OctNode*)buffer.GetUnsafePtr(),
            OutputBytes = outputBytes
        };
        
        return job.Schedule(dependency);
    }
    
    [BurstCompile]
    private struct DerasterizeToOctreeJob : IJob {
        [NativeDisableUnsafePtrRestriction] public Voxel* GridPtr;
        [NativeDisableUnsafePtrRestriction] public OctNode* bufferPtr;
        private ushort bufferPos;
        
        public NativeList<byte> OutputBytes;
        
        public void Execute() {
            bufferPos = MAX_NODES_OCTREE;

            //grid = level0
            OctNode* level1 = stackalloc OctNode[16 * 16 * 16]; // z order
            OctNode* level2 = stackalloc OctNode[8 * 8 * 8];    // z order
            OctNode* level3 = stackalloc OctNode[4 * 4 * 4];    // z order
            OctNode* level4 = stackalloc OctNode[2 * 2 * 2];    // z order
            
            OctNode* children = stackalloc OctNode[8];
            for (uint x = 0; x < 16; x++)
            for (uint y = 0; y < 16; y++)
            for (uint z = 0; z < 16; z++) {
                uint vx = x * 2;
                uint vy = y * 2;
                uint vz = z * 2;
                
                uint voxelIndex = NativeGrid.GetVoxelIndex(vz, vy, vx); 

                for (int i = 0; i < 8; i++) {
                    Voxel voxel = GridPtr[voxelIndex + i];
                    children[i] = new OctNode {
                        type = voxel.type,
                        density = voxel.density,
                        childIndex = 0
                    };
                }

                uint l1Idx = NativeGrid.GetVoxelIndex(z, y, x);
                level1[l1Idx] = CreateParentNode(children);
            }
            
            ProcessIntermediaryLevel(level1, level2, children, 16);
            ProcessIntermediaryLevel(level2, level3, children, 8);
            ProcessIntermediaryLevel(level3, level4, children, 4);
            
            OctNode root = CreateParentNode(level4);
            
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
        private void ProcessIntermediaryLevel(OctNode* source, OctNode* destination, OctNode* childBuffer, int sourceDimensions) {
            for (uint x = 0; x < (uint)sourceDimensions; x+=2)
            for (uint y = 0; y < (uint)sourceDimensions; y+=2)
            for (uint z = 0; z < (uint)sourceDimensions; z+=2) {
                uint voxelIndex = NativeGrid.GetVoxelIndex(z, y, x);
                for (int i = 0; i < 8; i++) {
                    childBuffer[i] = source[voxelIndex + i];
                }
                
                uint dstIdx = NativeGrid.GetVoxelIndex(z >> 1, y >> 1, x >> 1);
                destination[dstIdx] = CreateParentNode(childBuffer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private OctNode CreateParentNode(OctNode* children)
        { 
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
}