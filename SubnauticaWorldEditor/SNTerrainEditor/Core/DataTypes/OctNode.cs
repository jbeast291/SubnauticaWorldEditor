using System.Runtime.InteropServices;

namespace SNTerrainEditor.Core.DataTypes;

[StructLayout(LayoutKind.Sequential, Size = 4, Pack = 1)]
public struct OctNode
{
    public byte type;
    public byte density;
    public ushort childIndex;
}