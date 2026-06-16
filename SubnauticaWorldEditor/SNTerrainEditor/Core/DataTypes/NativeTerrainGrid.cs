using System;
using System.Runtime.CompilerServices;
using SNTerrainEditor.Extensions;
using Unity.Collections;
using WorldStreaming;

namespace SNTerrainEditor.Core.DataTypes;

public partial class NativeTerrainGrid(Int3 gridGlobalIndex) : IDisposable
{
    public const int SideLength = 32;
    public const int GridArrayLength = SideLength * SideLength * SideLength;

    public Int3 GridGlobalIndex { get; } = gridGlobalIndex;
    
    private Octree _associatedOctree;
    
    private GridStatus _status;
    private NativeArray<byte> _densityGrid;
    private NativeArray<byte> _typeGrid;

    public void SetGridsByOctree(Octree octree)
    {
        if(_status == GridStatus.Loaded) Dispose();
        
        octree.RasterizeToGrid(out _densityGrid, out _typeGrid);
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
    public static int GetBlockIndex(int x, int y, int z) => x + y * SideLength + z * SideLength * SideLength;
    
    public byte GetDensity(int x, int y, int z) => _densityGrid[GetBlockIndex(x, y, z)];
    public byte GetDensity(int index) => _densityGrid[index];
    public byte SetDensity(byte val, int x, int y, int z) => _densityGrid[GetBlockIndex(x, y, z)] = val;
    public byte SetDensity(byte val, int index) => _densityGrid[index] = val;
    
    public byte GetType(int x, int y, int z) => _typeGrid[GetBlockIndex(x, y, z)];
    public byte GetType(int index) => _typeGrid[index];
    public byte SetType(byte val, int x, int y, int z) => _typeGrid[GetBlockIndex(x, y, z)] = val;
    public byte SetType(byte val, int index) => _typeGrid[index] = val;

    public void UpdateAssociatedOctree()
    {
        _associatedOctree.data.Dispose();
        _associatedOctree.data = this.GetAsOctreeBytes();
    }
    
    public void DEBUG__ModifyWithLavaTexture()
    {
        for (int i = 0; i < _typeGrid.Length; i++)
        {
            if (_typeGrid[i] != 0)
            {
                _typeGrid[i] = 4;
            }
        }
    }

    public void Dispose()
    {
        _densityGrid.Dispose();
        _typeGrid.Dispose();
    }
    
    public enum GridStatus
    {
        Unloaded,
        Loaded,
    }
}