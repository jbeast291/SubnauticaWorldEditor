using SNTerrainEditor.Core.DataTypes;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Math = System.Math;

namespace SNTerrainEditor.Core.Editing;

internal class SdfSphereEdit : VoxelEdit
{
    internal SdfSphereEdit(NativeGrid grid, Int3 brushBlockPos, Int3 gridBlockPos)
        : base(grid, brushBlockPos, gridBlockPos) {
    }
    
    [BurstCompile]
    internal unsafe struct DensityAddSubJob: IJobParallelFor 
    { 
        [NativeDisableUnsafePtrRestriction] internal Voxel* grid;
        [ReadOnly] internal Int3 brushBlockPos;
        [ReadOnly] internal Int3 gridBlockPos;
        
        private const int radius = 16;
        private const int radiusSqr = radius * radius;
        private const float invRadiusSqr = 1.0f / radiusSqr;
        
        public void Execute(int index) {
            int z =   (index & 0x0001)
                    | ((index >> 2) & 0x0002)
                    | ((index >> 4) & 0x0004)
                    | ((index >> 6) & 0x0008)
                    | ((index >> 8) & 0x0010);
            int y =   ((index >> 1) & 0x0001)
                    | ((index >> 3) & 0x0002)
                    | ((index >> 5) & 0x0004)
                    | ((index >> 7) & 0x0008)
                    | ((index >> 9) & 0x0010);
            int x =   ((index >> 2) & 0x0001)
                    | ((index >> 4) & 0x0002)
                    | ((index >> 6) & 0x0004)
                    | ((index >> 8) & 0x0008)
                    | ((index >> 10) & 0x0010);
        
            //int sqrDist = Int3.SquareDistance(brushBlockPos, block);
            
            x = gridBlockPos.x + x;
            y = gridBlockPos.y + y;
            z = gridBlockPos.z + z;
            
            int num = brushBlockPos.x - x;
            int num2 = brushBlockPos.y - y;
            int num3 = brushBlockPos.z - z;
            int sqrDist = num * num + num2 * num2 + num3 * num3;
            
            if (sqrDist >= radiusSqr) return;

            // Normalize distance between [0.0-1.0]
            // Not a perfect replacement for sqrt but close enough, worth the performance
            float t = 1.0f - sqrDist * invRadiusSqr;

            // Scale to 0-252 density
            byte targetDensity = (byte)(t * 252f);

            if (targetDensity > 0) {
                grid[index].density = (byte)math.max((int)grid[index].density, targetDensity);
                grid[index].type = 4;
            }
        }

        /*private Int3 IndexToBlockPos(int index) {
            NativeGrid.GetVoxelCoordinates((uint)index, out uint z, out uint y, out uint x);
            return gridBlockPos + new Int3((int)x, (int)y, (int)z);
        }*/
    }

    public override unsafe JobHandle Schedule(JobHandle dependency)
    {
        DensityAddSubJob job = new() {
            grid = grid.GridPtr,
            brushBlockPos = BrushBlockPos,
            gridBlockPos = GridBlockPos
        };
        
        jobHandle = job.Schedule(
            NativeGrid.GridArrayLength, 
            NativeGrid.GridArrayLength / 64,
            dependency    
        );
        return jobHandle;
    }
}