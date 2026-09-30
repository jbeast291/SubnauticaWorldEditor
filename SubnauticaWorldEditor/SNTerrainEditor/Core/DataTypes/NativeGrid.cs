using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace SNTerrainEditor.Core.DataTypes;

internal unsafe partial class NativeGrid : IDisposable {
    internal const int SideLength = 32;
    internal const int GridArrayLength = SideLength * SideLength * SideLength;
    
    private readonly NativeArray<Voxel> _grid = new(GridArrayLength, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
    private readonly Voxel* _gridPtr;
    
    internal NativeGrid() {
        _gridPtr = (Voxel*) _grid.GetUnsafePtr();
    }
    
    internal Voxel* GridPtr => _gridPtr;
    
    private const uint layer1Mask = 0b_00001u;
    private const uint layer2Mask = 0b_00010u;
    private const uint layer3Mask = 0b_00100u;
    private const uint layer4Mask = 0b_01000u;
    private const uint layer5Mask = 0b_10000u;
    /// <summary>
    /// Z-order curve based indexing<br/>
    /// Maps regions of the grid to continuous blocks of memory that match the
    /// octree layout but in a dense grid
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint GetVoxelIndex(uint z, uint y, uint x) {
        // This only works cause the grid is 32x32x32 with 5 layers on powers of 2.
        // if that changes for some reason, this will need to be updated
        return   (z & layer1Mask)
               | (z & layer2Mask) << 2
               | (z & layer3Mask) << 4
               | (z & layer4Mask) << 6
               | (z & layer5Mask) << 8
               
               | (y & layer1Mask) << 1
               | (y & layer2Mask) << 3
               | (y & layer3Mask) << 5
               | (y & layer4Mask) << 7
               | (y & layer5Mask) << 9
               
               | (x & layer1Mask) << 2
               | (x & layer2Mask) << 4
               | (x & layer3Mask) << 6
               | (x & layer4Mask) << 8
               | (x & layer5Mask) << 10;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void GetVoxelCoordinates(uint index, out uint z, out uint y, out uint x)
    {
        z =    (index      & layer1Mask)
             | (index >> 2 & layer2Mask)
             | (index >> 4 & layer3Mask)
             | (index >> 6 & layer4Mask)
             | (index >> 8 & layer5Mask);

        y =    (index >> 1 & layer1Mask)
             | (index >> 3 & layer2Mask)
             | (index >> 5 & layer3Mask)
             | (index >> 7 & layer4Mask)
             | (index >> 9 & layer5Mask);

        x =    (index >> 2  & layer1Mask)
             | (index >> 4  & layer2Mask)
             | (index >> 6  & layer3Mask)
             | (index >> 8  & layer4Mask)
             | (index >> 10 & layer5Mask);
    }
    
    internal void DEBUG__Clear() {
        for (int i = 0; i < _grid.Length; i++) {
            ref Voxel voxel = ref _gridPtr[i];
            if (voxel.type != 0) {
                voxel.type = 0;
                voxel.density = 0;
            }
        }
    }

    public void Dispose() => _grid.Dispose();
}