using SNTerrainEditor.Core.DataTypes;
using Unity.Jobs;
using UnityEngine;

namespace SNTerrainEditor.Core.Editing;

internal abstract class VoxelEdit(NativeGrid grid, Int3 brushBlockPos, Int3 gridBlockPos)
{
    protected readonly NativeGrid grid = grid;
    protected readonly Int3 BrushBlockPos = brushBlockPos;
    protected readonly Int3 GridBlockPos = gridBlockPos;
    public JobHandle jobHandle;

    public abstract void Schedule();
    public virtual void Cleanup() { }
}