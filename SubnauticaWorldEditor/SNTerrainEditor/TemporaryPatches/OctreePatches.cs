using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using SNTerrainEditor.Core;
using SNTerrainEditor.Core.DataTypes;
using Unity.Collections;
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

    [HarmonyPatch(typeof(BatchOctrees), MethodType.Constructor, [
        typeof(BatchOctreesStreamer), typeof(Int3),
        typeof(int), typeof(UWE.SplitNativeArrayPool<byte>)
    ])]
    [HarmonyPostfix]
    public static void Prefix(BatchOctrees __instance)
    {
        __instance.allocator = FakeArrayPool.allocator;
    }
}


internal sealed class FakeArrayPool() : SplitNativeArrayPool<byte>(0, 0, 0, 0, 0, 0, 0, 0) {

    internal static readonly FakeArrayPool allocator = new();
    

    [HarmonyPatch(typeof(SplitNativeArrayPool<byte>), nameof(Get))]
    private static class FakeArrayPoolGetImpl {
        private static bool Prefix(
            SplitNativeArrayPool<byte> __instance, int minLength, ref NativeArray<byte> __result
        ) {
            if (__instance is not FakeArrayPool) return true;
            __result = new(minLength, Allocator.Persistent);
            return false;
        }
    }

    [HarmonyPatch(typeof(SplitNativeArrayPool<byte>), nameof(Return))]
    private static class FakeArrayPoolReturnImpl {
        private static bool Prefix(
            SplitNativeArrayPool<byte> __instance, ref NativeArray<byte> arr
        ) {
            if (__instance is not FakeArrayPool fakeArrayPool) return true;
            arr.Dispose();
            return false;
        }
    }
}