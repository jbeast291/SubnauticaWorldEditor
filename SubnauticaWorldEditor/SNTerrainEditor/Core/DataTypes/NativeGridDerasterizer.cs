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
    /// <summary>
    /// Schedules the octree derasterization job.
    /// </summary>
    public static JobHandle Schedule(NativeGrid grid, NativeList<OctNode> octTree, JobHandle dependency = default) {
        DerasterizeToOctreeJob job = new() {
            GridPtr = grid.GridPtr,
            OutputBytes = octTree
        };
        
        return job.Schedule(dependency);
    }

    private struct DerasterizeToOctreeJob : IJob
    {
        [NativeDisableUnsafePtrRestriction] public Voxel* GridPtr;
        public NativeList<OctNode> OutputBytes;

        private ushort OutputNodeCount;
        private ushort outputIndex;
        private OctNode* outputPtr;

        public void Execute()
        {
            //Root node is always present, so initialize count to always start with 1
            OutputNodeCount = 1;
            
            // each byte represents if the 8 children below are uniform
            // bit 1 is uniform
            // bit 0 is non-uniform (needs to be written to tree)
            const int L1_UNIFORMITY_SIZE = (16 * 16 * 16) / 8; // 8 is the packing part
            byte* uniformityBufferL1Ptr = stackalloc byte[L1_UNIFORMITY_SIZE];
            
            const int L2_UNIFORMITY_SIZE = (8 * 8 * 8) / 8;
            byte* uniformityBufferL2Ptr = stackalloc byte[L2_UNIFORMITY_SIZE];
            
            for (ushort i = 0; i < L1_UNIFORMITY_SIZE; i++) {
                uniformityBufferL1Ptr[i] = L1Uniformity(GridPtr + (i * 64));
            }
            for (ushort i = 0; i < L2_UNIFORMITY_SIZE; i++) {
                uniformityBufferL2Ptr[i] = IntermediaryUniformity(uniformityBufferL1Ptr + (i * 8));
            }
            
            byte l3Uniformity = IntermediaryUniformity(uniformityBufferL2Ptr);
            if (l3Uniformity != 0b_1111_1111) OutputNodeCount += 8;
            
            OutputBytes.ResizeUninitialized(OutputNodeCount);
            outputIndex = OutputNodeCount;
            Plugin.LogError("Before" + outputIndex );
            //safe to do since OctNode is aligned to bytes
            outputPtr = (OctNode*)OutputBytes.GetUnsafePtr();

            OutputBytes[0] = new OctNode {
                type = 0,
                density = 0,
                childIndex = 0
            };

            Plugin.LogError("After" + outputIndex );
        }

        // Pass in 64 Voxels
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte L1Uniformity(Voxel* childrenGroups)
        {
            byte packedChildUniformity = 0b_0000_0000;
            for (int childIdx = 0; childIdx < 8; childIdx++) {
                Voxel* group = childrenGroups + (childIdx * 8);
                Voxel first = group[0];
                
                bool isUniform = group[1].density == first.density && group[1].type == first.type &&
                                 group[2].density == first.density && group[2].type == first.type &&
                                 group[3].density == first.density && group[3].type == first.type &&
                                 group[4].density == first.density && group[4].type == first.type &&
                                 group[5].density == first.density && group[5].type == first.type &&
                                 group[6].density == first.density && group[6].type == first.type &&
                                 group[7].density == first.density && group[7].type == first.type;
                
                if (isUniform) packedChildUniformity |= (byte)(0b_1000_0000 >> childIdx); 
                else OutputNodeCount += 8;
            }

            return packedChildUniformity;
        }

        // Pass in 8 Uniformity maps
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte IntermediaryUniformity(byte* packedGroups)
        {
            byte packedChildUniformity = 0b_0000_0000;

            for (int childIdx = 0; childIdx < 8; childIdx++) {
                if (packedGroups[childIdx] == 0b_1111_1111) {
                    packedChildUniformity |= (byte)(0b_1000_0000 >> childIdx);
                }
                else OutputNodeCount += 8;
            }

            return packedChildUniformity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private OctNode CreateParentNodeVoxel(Voxel* children)
        {
            ushort densitySum = 0;
            for (int i = 0; i < 8; i++)
            {
                Voxel current = children[i];
                densitySum += (current.density == 0 && current.type != 0)
                    ? (byte)252
                    : current.density;
            }

            byte avgDensity = (byte)(densitySum >> 3);
            byte dominantType = 0;

            if (avgDensity >= 126) dominantType = GetDominantType(children);

            outputIndex -= 8;
            for (int i = 0; i < 8; i++)
            {
                outputPtr[outputIndex + i] = new OctNode
                {
                    density = children[i].density,
                    type = children[i].type,
                    childIndex = 0
                };
            }

            return new OctNode
            {
                density = avgDensity,
                type = dominantType,
                childIndex = outputIndex,
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte GetDominantType(Voxel* children)
        {
            byte bestType = 0;
            int bestCount = 0;

            for (int i = 0; i < 8; i++)
            {
                byte type = children[i].type;
                if (type == 0) continue;

                int count = 1;
                for (int j = i + 1; j < 8; j++)
                {
                    count += children[j].type == type ? 1 : 0;
                }

                if (count > bestCount)
                {
                    bestCount = count;
                    bestType = type;
                    if (bestCount >= 4) break;
                }
            }

            return bestType;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte GetDominantTypeOctNode(OctNode* children)
        {
            byte bestType = 0;
            int bestCount = 0;

            for (int i = 0; i < 8; i++)
            {
                byte type = children[i].type;
                if (type == 0) continue;

                int count = 1;
                for (int j = i + 1; j < 8; j++)
                {
                    count += children[j].type == type ? 1 : 0;
                }

                if (count > bestCount)
                {
                    bestCount = count;
                    bestType = type;
                    if (bestCount >= 4) break;
                }
            }

            return bestType;
        }
    }
}