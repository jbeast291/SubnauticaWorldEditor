namespace SNTerrainEditor.Extensions;

public static class Int3Extensions
{
    extension(Int3 int3)
    {
        public static Int3 PositiveModulo(Int3 a, int b)
        {
            return new Int3(UWE.Math.PositiveModulo(a.x, b), UWE.Math.PositiveModulo(a.y, b), UWE.Math.PositiveModulo(a.z, b));
        }
    }
}