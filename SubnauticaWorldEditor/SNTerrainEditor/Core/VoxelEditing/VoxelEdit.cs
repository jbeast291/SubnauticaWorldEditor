using SNTerrainEditor.Core.DataTypes;
using Unity.Jobs;
using UnityEngine;

namespace SNTerrainEditor.Core.Editing;

internal abstract class VoxelEdit(NativeGrid grid, Int3 gridBlockPos, Int3 brushBlockPos, int brushScale)
{
    protected readonly NativeGrid grid = grid;
    protected readonly Int3 GridBlockPos = gridBlockPos;
    protected readonly Int3 BrushBlockPos = brushBlockPos;
    protected readonly int BrushScale = brushScale;

    public JobHandle jobHandle;

    public abstract JobHandle Schedule(JobHandle dependency);
    public virtual void Cleanup() { }
}