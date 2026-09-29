using SNTerrainEditor.Core.DataTypes;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using UWE;
using Math = System.Math;

namespace SNTerrainEditor.Core.Editing;

internal class SdfSphereEdit : VoxelEdit
{
    internal SdfSphereEdit(NativeGrid grid, Int3 brushBlockPos, Int3 gridBlockPos)
        : base(grid, brushBlockPos, gridBlockPos) {
    }

    private readonly unsafe struct DensityAddSubJob(
        Voxel* grid, Int3 brushBlockPos, Int3 gridGlobalIndex
    ) : IJobFor
    {
        private const int radius = 16;
        private const int radiusSqr = radius * radius;
        private const float invRadiusSqr = 1.0f / radiusSqr;
        
        public void Execute(int index) {
            Int3 block = IndexToBlockPos(index);
        
            int sqrDist = Int3.SquareDistance(brushBlockPos, block);
            
            if (sqrDist >= radiusSqr) return;

            // Normalize distance between [0.0-1.0]
            // Not a perfect replacement for sqrt but close enough, worth the performance
            float t = 1.0f - sqrDist * invRadiusSqr;

            // Scale to 0-252 density
            byte targetDensity = (byte)(t * 252f);

            if (targetDensity > 0) {
                grid[index].density = Math.Max(grid[index].density, targetDensity);
                grid[index].type = 4;
            }
        }

        private Int3 IndexToBlockPos(int index) {
            NativeGrid.GetVoxelCoordinates((uint)index, out uint z, out uint y, out uint x);
            return gridGlobalIndex + new Int3((int)x, (int)y, (int)z);
        }
    }

    public override unsafe void Schedule()
    {
        DensityAddSubJob job = new(
            grid.GridPtr,
            BrushBlockPos,
            GridBlockPos
        );
        
        jobHandle = job.ScheduleParallel(
            NativeGrid.GridArrayLength, 
            NativeGrid.GridArrayLength/64, 
            new JobHandle()
        );
    }
}