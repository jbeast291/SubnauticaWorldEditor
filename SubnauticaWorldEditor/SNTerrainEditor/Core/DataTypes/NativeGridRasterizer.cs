using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace SNTerrainEditor.Core.DataTypes;

internal static unsafe class NativeGridRasiterizer {
    
    /// <summary>Rasterizes an octree into the NativeGrid</summary>
    public static JobHandle Schedule(NativeArray<byte> octreeData, NativeGrid grid, JobHandle dependency = default) {
        if (!octreeData.IsCreated || octreeData.Length == 0) return dependency;
        
        RasterizeToGridJob toGridJob = new() {
            Nodes = octreeData.Reinterpret<OctNode>(),
            GridPtr = (ushort*)grid.GridPtr,
        };
        
        return toGridJob.Schedule(dependency);
    }
    
    // Exactly 8 bytes per stack frame: Offset (4B) + Index (2B) + Layer (1B) + Pad (1B)
    private struct OctreeStackFrame {
        public uint GridOffset;
        public ushort NodeIndex;
        public byte Layer; // 0 = Root (width 32), 1 = (16), 2 = (8), 3 = (4), 4 = (2), 5 = (1)
    }

    [BurstCompile]
    private struct RasterizeToGridJob : IJob {
        [ReadOnly] internal NativeArray<OctNode> Nodes;
        [NativeDisableUnsafePtrRestriction] internal ushort* GridPtr;

        public void Execute() {
            if (Nodes.Length == 0) return;

            // 1 + (5 * 7) = 36 max concurrent stack items for a 6-layer octree
            const int MAX_STACK_ITEMS = 36;
            
            // Solution for non recursion with burst
            OctreeStackFrame* stack = stackalloc OctreeStackFrame[MAX_STACK_ITEMS];

            int stackPtr = 0;

            // Root Node
            stack[stackPtr] = new OctreeStackFrame { NodeIndex = 0, GridOffset = 0, Layer = 0 };
            stackPtr++;
            
            while (stackPtr > 0) {
                stackPtr--;
                OctreeStackFrame current = stack[stackPtr];
                OctNode node = Nodes[current.NodeIndex];
                ushort childIndex = node.childIndex;
                byte layer = current.Layer;
                
                if (childIndex == 0) {
                    ushort value = (ushort)(node.type | (node.density << 8));
                    ushort* destination = GridPtr + current.GridOffset;

                    if (layer == 5) {
                        *destination = value;
                    }
                    else {
                        int volume = GetVolumeForLayer(layer);
                        UnsafeUtility.MemCpyReplicate(destination, &value, sizeof(ushort), volume);
                    }
                    continue;
                }

                int childVolume = GetVolumeForLayer(layer + 1);
                
                for (int i = 7; i >= 0; i--) {
                    stack[stackPtr++] = new OctreeStackFrame {
                        NodeIndex = (ushort)(childIndex + i),
                        GridOffset = current.GridOffset + (uint)childVolume * (uint)i,
                        Layer = (byte)(layer + 1)
                    };
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetVolumeForLayer(int layer) => 1 << (15 - (3 * layer));
    }
}
