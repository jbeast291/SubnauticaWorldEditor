using System.Runtime.CompilerServices;
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
    
    private struct RasterizeToGridJob : IJob {
        [ReadOnly] internal NativeArray<OctNode> Nodes;
        [NativeDisableUnsafePtrRestriction] internal ushort* GridPtr;

        public void Execute() {
            if (Nodes.Length == 0) return;
            RasterRegion(0, 0, 0);
        }

        private void RasterRegion(uint gridOffset, ushort nodeIndex, byte layer) {
            OctNode node = Nodes[nodeIndex];
            ushort childIndex = node.childIndex;
                
            if (childIndex == 0) {
                ushort value = (ushort)(node.type | (node.density << 8));
                ushort* destination = GridPtr + gridOffset;

                if (layer == 5) {
                    *destination = value;
                }
                else {
                    int volume = GetVolumeForLayer(layer);
                    UnsafeUtility.MemCpyReplicate(destination, &value, sizeof(ushort), volume);
                }
                return;
            }
            int childVolume = GetVolumeForLayer(layer + 1);
            
            for (int i = 7; i >= 0; i--) {
                RasterRegion(gridOffset + (uint)childVolume * (uint)i, (ushort)(childIndex + i), (byte)(layer + 1));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetVolumeForLayer(int layer) => 1 << (15 - (3 * layer));
    }
}
