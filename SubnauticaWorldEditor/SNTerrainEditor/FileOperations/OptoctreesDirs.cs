using System;
using System.IO;

namespace SNTerrainEditor.FileOperations;

internal static class OptoctreesDirs {
    private static readonly string[] ORIG_BATCH_DIRS = ["Build18", "Expansion"];

    static OptoctreesDirs() {
        foreach (string origDirName in ORIG_BATCH_DIRS) {
            string? origDir = SNUtils.InsideUnmanaged(origDirName);

            if (!Directory.Exists(origDir)) continue;
            ORIG_PATH = Path.Combine(origDir, "CompiledOctreesCache"); 
            PATCHED_PATH = Path.Combine(ORIG_PATH, "patches");
            break;
        }

        if (ORIG_PATH == null || PATCHED_PATH == null) {
            throw new Exception("couldn't determine the batch directory");
        }
    }

    internal static string GetBatchPath(Int3 batchId) {
        string fileName = $"compiled-batch-{batchId.x}-{batchId.y}-{batchId.z}.optoctrees";
        return Path.Combine(ORIG_PATH, fileName);
    }

    internal static readonly string ORIG_PATH;
    internal static readonly string PATCHED_PATH;
}
