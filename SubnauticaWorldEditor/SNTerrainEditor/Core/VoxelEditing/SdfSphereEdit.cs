using SNTerrainEditor.Core.DataTypes;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Math = System.Math;

namespace SNTerrainEditor.Core.Editing;

internal class SdfSphereEdit : VoxelEdit
{
    internal SdfSphereEdit(NativeGrid grid, Int3 gridBlockPos, Int3 brushBlockPos, int brushScale) 
        : base(grid, gridBlockPos, brushBlockPos, brushScale) {
    }
    
    [BurstCompile]
    internal unsafe struct DensityAddSubJob: IJobParallelFor 
    { 
        [NativeDisableUnsafePtrRestriction] internal Voxel* grid;
        [ReadOnly] internal Int3 brushBlockPos;
        [ReadOnly] internal Int3 gridBlockPos;
        [ReadOnly] internal int radiusSqr;
        [ReadOnly] internal float invRadiusSqr;
        
        public void Execute(int index) {
            NativeGrid.GetVoxelCoordinates((uint)index, out uint uz, out uint uy, out uint ux);
            int z = (int)uz;
            int y = (int)uy;
            int x = (int)ux;
            
            x = gridBlockPos.x + x;
            y = gridBlockPos.y + y;
            z = gridBlockPos.z + z;
            
            int xdist = brushBlockPos.x - x;
            int ydist = brushBlockPos.y - y;
            int zdist = brushBlockPos.z - z;
            int sqrDist = xdist * xdist + ydist * ydist + zdist * zdist;
            
            if (sqrDist >= radiusSqr) return;

            // Normalize distance between [0.0-1.0]
            // Not a perfect replacement for sqrt but close enough, worth the performance
            float t = 1.0f - 0.5f * sqrDist * invRadiusSqr;
            
            // Scale to 0-252 density
            byte targetDensity = (byte)(t * 252f);

            if (targetDensity > 0) {
                grid[index].density = (byte)math.max((int)grid[index].density, targetDensity);
                grid[index].type = 4;
            }
        }
    }

    public override unsafe JobHandle Schedule(JobHandle dependency)
    {
        DensityAddSubJob job = new() {
            grid = grid.GridPtr,
            brushBlockPos = BrushBlockPos,
            gridBlockPos = GridBlockPos,
            radiusSqr = BrushScale * BrushScale,
            invRadiusSqr = 1.0f / (BrushScale * BrushScale),
        };
        
        jobHandle = job.Schedule(
            NativeGrid.GridArrayLength, 
            NativeGrid.GridArrayLength / 64,
            dependency    
        );
        return jobHandle;
    }
}