using System;
using SNTerrainEditor.Core.DataTypes;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace SNTerrainEditor.Extensions;

#pragma warning disable Publicizer001
public static class VoxelGridAllocator {
    private const int VoxelGridAlignment = 32;
    public static unsafe NativeArray<Voxel> WithAlignment(int length, Allocator allocator)
    {
        long size = (long)UnsafeUtility.SizeOf<Voxel>() * length;
        NativeArray<Voxel> array = default;
        array.m_Buffer = UnsafeUtility.Malloc(size, VoxelGridAlignment, allocator);
        array.m_Length = length;
        array.m_AllocatorLabel = allocator;
        return array;
    }
}