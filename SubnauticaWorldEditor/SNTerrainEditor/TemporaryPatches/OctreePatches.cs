using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using SNTerrainEditor.Core;
using SNTerrainEditor.Core.DataTypes;
using UWE;
using WorldStreaming;

namespace SNTerrainEditor.TemporaryPatches;

[HarmonyPatch(typeof(Octree))]
public static class OctreePatches
{
    [HarmonyPatch(typeof(Octree), nameof(Octree.Read))]
    [HarmonyPrefix]
    private static bool Prefix(Octree __instance, Int3 batchId, SplitNativeArrayPool<byte> allocator)
    {
        if (TEBatchManager.main.activeSession.managedBatches.TryGetValue(batchId, 
                out ManagedBatch managedBatch)
        ) {
            __instance.data = managedBatch.octrees.Get(__instance.id).arr;
            return false;
        }
        return true;
    }
}


//TODO: might be smarter to patch octree?
[HarmonyPatch(typeof(BatchOctrees))]
public static class BatchOctreePatches2
{
    // io thread >_> 
    [HarmonyPatch(typeof(BatchOctrees), nameof(BatchOctrees.ClearOctrees))]
    [HarmonyPrefix]
    public static bool Prefix(BatchOctrees __instance)
    {
        if (TEBatchManager.main.activeSession.managedBatches.TryGetValue(__instance.id, out ManagedBatch _)
        ) {
            __instance.streamer.streamingThread.Enqueue(BatchOctrees.EndUnloadOctreesDelegate, __instance, null);
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(BatchOctrees))]
public static class BatchOctreePatches
{
    [HarmonyPatch(typeof(BatchOctrees), MethodType.Constructor, [
        typeof(BatchOctreesStreamer), typeof(Int3),
        typeof(int), typeof(UWE.SplitNativeArrayPool<byte>)
    ])]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) 
        => new CodeMatcher(instructions)
            .MatchForward(
                useEnd: false,
                new CodeMatch(OpCodes.Ldarg_2),
                new CodeMatch(OpCodes.Newobj, AccessTools.Constructor(typeof(Octree), [typeof(Int3)]))
            )
            .SetOpcodeAndAdvance(OpCodes.Ldloc_2)
            .InstructionEnumeration();
}