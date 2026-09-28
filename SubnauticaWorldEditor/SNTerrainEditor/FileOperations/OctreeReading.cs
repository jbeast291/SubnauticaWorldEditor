using System;
using System.IO;
using SNTerrainEditor.Core.DataTypes;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using WorldStreaming;

namespace SNTerrainEditor.FileOperations;

public static class OctreeReading {

    private static readonly NativeArray<byte> empty = new(0, Allocator.Persistent);
    private static readonly byte[] readBuffer = new byte[65536];

    internal static ManagedBatch GetBatchOctrees(Int3 id) {
        string path = OptoctreesDirs.GetBatchPath(id);
        ManagedBatch batchdata = new();
        
        if (TryReadBatchFile(id, path, batchdata)) return batchdata;

        GenerateEmptyBatch(batchdata);
        return batchdata;
    }

    private static void GenerateEmptyBatch(ManagedBatch batchdata) {
        foreach (Int3 octreeLocalIndex in Int3.Range(batchdata.octrees.Length)) {
            if (batchdata.octrees.data != null)
            {
                throw new InvalidDataException("Cannot overwrite existing batch data with empty");
            }
            batchdata.octrees.Get(octreeLocalIndex).octreeBytes = empty;
        }
    }
    
    private static bool TryReadBatchFile(
        Int3 id, string path, ManagedBatch batchdata
    ) {
        if (!File.Exists(path)) return false;

        if (id.x == 25 || id.z == 25) return false;

        try {
            using PooledBinaryReader reader = new(File.OpenRead(path));

            if (reader.ReadInt32() < 4) return false; // version field

            foreach (Int3 octreeLocalIndex in Int3.Range(ManagedBatch.OCTREES_PER_SIDE))
            {
                batchdata.octrees.Get(octreeLocalIndex).octreeBytes = ReadOctree(reader);
            }
        }
        catch (Exception ex) {
            Plugin.LogError(
                $"Exception while loading octrees, batch {id}, path '{path}', exception {ex}"
            );
            return false;
        }
        return true;
    }


    private static unsafe NativeArray<byte> ReadOctree(PooledBinaryReader reader) {
        int num = reader.ReadUInt16() * 4;
        if (num == 0) {
            return empty;
        }
        NativeArray<byte> data = new(num, Allocator.Persistent);
        lock (readBuffer) {
            int num2 = 0;
            if (num <= readBuffer.Length) {
                num2 = reader.Read(readBuffer, 0, num);
                fixed (byte* ptr = readBuffer) {
                    void* source = ptr;
                    UnsafeUtility.MemCpy(NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(data), source, num2);
                }
                return data;
            }
            int num3 = num;
            int num4;
            do {
                int count = Math.Min(num3, readBuffer.Length);
                if ((num4 = reader.Read(readBuffer, 0, count)) <= 0) continue;
                
                fixed (byte* ptr = readBuffer) {
                    void* source2 = ptr;
                    UnsafeUtility.MemCpy(NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(data.GetSubArray(num2, num4)), source2, num4);
                }
                num2 += num4;
                num3 -= num4;
            }
            while (num4 > 0 && num2 < num);
        }
        return data;
    }
}