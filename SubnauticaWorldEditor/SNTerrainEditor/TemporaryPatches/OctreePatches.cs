using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using BepInEx.Logging;
using HarmonyLib;
using Unity.Collections;
using UWE;
using WorldStreaming;

namespace SNTerrainEditor.TemporaryPatches;

[HarmonyPatch(typeof(Octree))]
public static class OctreePatches
{
    [HarmonyPatch(typeof(Octree), nameof(Octree.Read))]
    [HarmonyPrefix]
    private static bool PrefixRead(Octree __instance, Int3 batchId)
    {
        if (batchId != new Int3(12, 18, 12)) return true;
        Int3 octreeId = (batchId * 5) + __instance.id;
        Plugin.LogError("Reading " + octreeId);
        Logger.Listeners.OfType<DiskLogListener>().ForEach(l => l.LogWriter?.Flush());
        __instance.data = LibOptoctrees.GetOctreeBlocking(octreeId);
        Plugin.LogError("Read " + __instance.data.Length + " " + __instance.data[0]);
        Logger.Listeners.OfType<DiskLogListener>().ForEach(l => l.LogWriter?.Flush());
        return false;
    }

    [HarmonyPatch(typeof(Octree), nameof(Octree.Clear))]
    [HarmonyPrefix]
    public static bool PrefixClear(Octree __instance)
    {
        //LibOptoctrees.DropRustOctree(__instance.data);
        return false;
    }

    [HarmonyPatch(typeof(Octree), nameof(Octree.MakeLeaf))]
    [HarmonyPrefix]
    public static bool PrefixMakeLeaf(Octree __instance) 
        => throw new Exception("Modification unsupported on rust allocated octree!");

    [HarmonyPatch(typeof(Octree), nameof(Octree.UnloadChildren))]
    [HarmonyPrefix]
    public static bool Prefix(Octree __instance) => false;
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