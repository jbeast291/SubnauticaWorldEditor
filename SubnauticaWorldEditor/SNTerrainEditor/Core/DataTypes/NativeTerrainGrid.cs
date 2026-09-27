using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SNTerrainEditor.Extensions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using WorldStreaming;

namespace SNTerrainEditor.Core.DataTypes;

public unsafe partial class NativeTerrainGrid(Int3 gridGlobalIndex) : IDisposable
{
    public const int SideLength = 32;
    public const int GridArrayLength = SideLength * SideLength * SideLength;

    public Int3 GridGlobalIndex { get; } = gridGlobalIndex;
    
    private Octree _associatedOctree;
    
    private GridStatus _status;
    private NativeArray<Voxel> _grid;
    private Voxel* _gridPtr;

    public void SetGridsByOctree(Octree octree)
    {
        if(_status == GridStatus.Loaded) Dispose();
        _grid = octree.RasterizeToGrid();
        _gridPtr = (Voxel*)_grid.GetUnsafePtr();
        
        _associatedOctree = octree;
        _status = GridStatus.Loaded;
    }

    public void UnloadGrid()
    {
        Plugin.Logger.LogError("DISPOSING GRIDS");
        Dispose();
        _associatedOctree = null;
        _status = GridStatus.Unloaded;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetVoxelIndex(int x, int y, int z) => x + y * SideLength + z * SideLength * SideLength;
    /*
    public byte GetDensity(int x, int y, int z) => _densityGrid[GetVoxelIndex(x, y, z)];
    public byte GetDensity(int index) => _densityGrid[index];
    public byte SetDensity(byte val, int x, int y, int z) => _densityGrid[GetVoxelIndex(x, y, z)] = val;
    public byte SetDensity(byte val, int index) => _densityGrid[index] = val;
    
    public byte GetType(int x, int y, int z) => _typeGrid[GetVoxelIndex(x, y, z)];
    public byte GetType(int index) => _typeGrid[index];
    public byte SetType(byte val, int x, int y, int z) => _typeGrid[GetVoxelIndex(x, y, z)] = val;
    public byte SetType(byte val, int index) => _typeGrid[index] = val;
    */
    public void UpdateAssociatedOctree()
    {
        _associatedOctree.data.Dispose();
        _associatedOctree.data = this.GetAsOctreeBytes();
    }
    
    public unsafe void DEBUG__ModifyWithLavaTexture()
    {
        Voxel* ptr = (Voxel*)_grid.GetUnsafePtr();
        for (int i = 0; i < _grid.Length; i++)
        {
            ref Voxel voxel = ref ptr[i];
            if (voxel.type != 0)
            {
                voxel.type = 4;
            }
        }
    }

    public void Dispose()
    {
        _grid.Dispose();
    }
    
    public enum GridStatus
    {
        Unloaded,
        Loaded,
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct Voxel
{
    public byte type;
    /// <summary>
    /// 0: (when the material != 0) means voxel is fully solid<br/>
    /// 1-125: above the surface<br/>
    /// 126: at the surface<br/>
    /// 127-252: below the surface
    /// </summary>
    public byte density;
}