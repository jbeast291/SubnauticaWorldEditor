using Unity.Mathematics;

namespace SNTerrainEditor.Extensions;

public static class Int3Extensions
{
    extension(Int3 instance) {
        public int3 ToBurstInt3() => new(instance.x, instance.y, instance.z);
    }    
}