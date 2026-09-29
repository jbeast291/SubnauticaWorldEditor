using System;
using System.IO;
using SNTerrainEditor.Core.DataTypes;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace SNTerrainEditor.FileOperations;

public static class OctreeReading {
    
    internal static readonly NativeArray<byte> empty = new(0, Allocator.Persistent);
    
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
        int bytesToRead = reader.ReadUInt16() * 4;
        if (bytesToRead == 0) return empty;
        NativeArray<byte> data = new(bytesToRead, Allocator.Persistent);
        lock (readBuffer) {
            int totalBytesRead = 0;
            if (bytesToRead <= readBuffer.Length) {
                totalBytesRead = reader.Read(readBuffer, 0, bytesToRead);
                fixed (byte* ptr = readBuffer) {
                    UnsafeUtility.MemCpy(NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(data), ptr, totalBytesRead);
                }
                return data;
            }
            int bytesRemaining = bytesToRead;
            int bytesReadToBuffer;
            do {
                int count = Math.Min(bytesRemaining, readBuffer.Length);
                if ((bytesReadToBuffer = reader.Read(readBuffer, 0, count)) <= 0) continue;
                fixed (byte* ptr = readBuffer) {
                    UnsafeUtility.MemCpy(NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(data.GetSubArray(totalBytesRead, bytesReadToBuffer)), ptr, bytesReadToBuffer);
                }
                totalBytesRead += bytesReadToBuffer;
                bytesRemaining -= bytesReadToBuffer;
            }
            while (bytesReadToBuffer > 0 && totalBytesRead < bytesToRead);
        }
        return data;
    }
}