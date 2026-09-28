using System;
using System.Runtime.CompilerServices;
using SNTerrainEditor.Extensions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using WorldStreaming;

namespace SNTerrainEditor.Core.DataTypes;

public unsafe partial class NativeGrid : IDisposable
{
    public const int SideLength = 32;
    private const int GridArrayLength = SideLength * SideLength * SideLength;
    
    private NativeArray<Voxel> _grid = new(GridArrayLength, Allocator.Persistent);
    private Voxel* _gridPtr;

    public void SetFromOctree(NativeArray<byte> octreeData) {
        _gridPtr = (Voxel*)_grid.GetUnsafePtr();
        OctreeRasterizer.RasterizeToGrid(octreeData, _gridPtr);
    }
    
    /// <summary>
    /// Z-order curve based indexing<br/>
    /// Maps regions of the grid to continuous blocks of memory that match the
    /// octree layout but in a dense grid
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetVoxelIndex(uint z, uint y, uint x)
    {
        // This only works cause the grid is 32x32x32 with 5 layers on powers of 2.
        // if that changes for some reason, this will need to be updated
        const uint layer1Mask = 0b_00001u;
        const uint layer2Mask = 0b_00010u;
        const uint layer3Mask = 0b_00100u;
        const uint layer4Mask = 0b_01000u;
        const uint layer5Mask = 0b_10000u;
        return   (z & layer1Mask)
               | (y & layer1Mask) << 1
               | (x & layer1Mask) << 2
               
               | (z & layer2Mask) << 2
               | (y & layer2Mask) << 3
               | (x & layer2Mask) << 4
               
               | (z & layer3Mask) << 4
               | (y & layer3Mask) << 5
               | (x & layer3Mask) << 6
               
               | (z & layer4Mask) << 6
               | (y & layer4Mask) << 7
               | (x & layer4Mask) << 8
               
               | (z & layer5Mask) << 8
               | (y & layer5Mask) << 9
               | (x & layer5Mask) << 10;
    }
    
    public void DEBUG__ModifyWithLavaTexture() {
        for (int i = 0; i < _grid.Length; i++)
        {
            ref Voxel voxel = ref _gridPtr[i];
            if (voxel.type != 0)
            {
                voxel.type = 4;
            }
        }
    }

    public void Dispose() => _grid.Dispose();
}