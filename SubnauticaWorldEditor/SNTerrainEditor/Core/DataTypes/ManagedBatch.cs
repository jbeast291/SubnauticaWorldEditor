using Unity.Collections;

namespace SNTerrainEditor.Core.DataTypes;

internal class ManagedBatch {
    public const int OCTREES_PER_SIDE = 5;
    
    internal readonly Array3<ManagedOctree> octrees = new(5);
    internal ManagedBatch() {
        foreach (Int3 index in Int3.Range(OCTREES_PER_SIDE)) {
            octrees.Set(index, new());
        }
    }
}


internal class ManagedOctree {
    internal NativeArray<byte> octreeBytes;
}