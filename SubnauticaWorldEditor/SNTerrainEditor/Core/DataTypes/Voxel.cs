using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SNTerrainEditor.Core.DataTypes;

[StructLayout(LayoutKind.Explicit, Size = 2, Pack = 2)]
public struct Voxel : IEquatable<Voxel>
{
    [FieldOffset(0)] public byte type;
    /// <summary>
    /// 0: (when the material != 0) means voxel is fully solid<br/>
    /// 1-125: above the surface<br/>
    /// 126: at the surface<br/>
    /// 127-252: below the surface
    /// </summary>
    [FieldOffset(1)] public byte density;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Voxel other) 
        => Unsafe.As<Voxel, ushort>(ref this) == Unsafe.As<Voxel, ushort>(ref other);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object obj) => obj is Voxel other && Equals(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => (density << 8) | type;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Voxel left, Voxel right) 
        => Unsafe.As<Voxel, ushort>(ref left) == Unsafe.As<Voxel, ushort>(ref right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Voxel left, Voxel right)
        => Unsafe.As<Voxel, ushort>(ref left) != Unsafe.As<Voxel, ushort>(ref right);
}