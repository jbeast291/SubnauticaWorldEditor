using SNTerrainEditor.Core.DataTypes;
using SNTerrainEditor.Extensions;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace SNTerrainEditor.Core.Editing;

internal class SdfSphereEdit : VoxelEdit
{
    internal SdfSphereEdit(NativeGrid grid, Int3 gridBlockPos, Int3 brushBlockPos, float brushScale) 
        : base(grid, gridBlockPos, brushBlockPos, brushScale) {
    }
    
    [BurstCompile]
    private unsafe struct DensityAddJob<TDensity>: IJobParallelFor where TDensity : struct, IDensityFunction
    { 
        [NativeDisableUnsafePtrRestriction] internal Voxel* grid;
        [ReadOnly] internal int3 shapeCenter;
        [ReadOnly] internal int3 gridBlockPos;
        [ReadOnly] internal TDensity densityFunc;

        
        public void Execute(int index) {
            int3 localBlockPos = NativeGrid.GetLocalVoxelIdx(index);
            
            float t = densityFunc.Evaluate(shapeCenter, gridBlockPos + localBlockPos);
            
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
        /*
        float scale = BrushScale / 2.0f;
        float sqrScale = scale * scale;

        DensityAddJob<SphereDensityFunction> job = new() {
            grid = grid.GridPtr,
            shapeCenter = BrushBlockPos.ToBurstInt3(),
            gridBlockPos = GridBlockPos.ToBurstInt3(),
            densityFunc = new SphereDensityFunction() {
                sqrScale = sqrScale,
                invSqrScale = 1.0f / sqrScale
            },
        };
        */

        DensityAddJob<PyramidDensityFunction> job = new() {
            grid = grid.GridPtr,
            shapeCenter = BrushBlockPos.ToBurstInt3(),
            gridBlockPos = GridBlockPos.ToBurstInt3(),
            densityFunc = new PyramidDensityFunction() {
                height = BrushScale,
                baseHalfWidth = BrushScale/4,
            },
        };
        
        jobHandle = job.Schedule(
            NativeGrid.GridArrayLength, 
            NativeGrid.GridArrayLength / 64,
            dependency    
        );
        return jobHandle;
    }
}