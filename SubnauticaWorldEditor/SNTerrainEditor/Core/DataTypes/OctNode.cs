using System.Runtime.InteropServices;

namespace SNTerrainEditor.Core.DataTypes;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct OctNode
{
    public byte type;
    public byte density;
    public ushort childIndex;
}