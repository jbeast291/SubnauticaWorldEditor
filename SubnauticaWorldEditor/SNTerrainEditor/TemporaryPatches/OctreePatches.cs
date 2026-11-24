using System;
using System.Collections.Generic;
using System.Reflection.Emit;
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
        Int3 octreeId = (batchId * 5) + __instance.id;
        __instance.data = LibOptoctrees.GetRustOctreeBlocking(octreeId);
        return false;
    }

    [HarmonyPatch(typeof(Octree), nameof(Octree.MakeLeaf))]
    [HarmonyPrefix]
    public static void PrefixMakeLeaf(Octree __instance) {
        if (__instance.data.m_AllocatorLabel == Allocator.RustSharedOctree) { 
            throw new Exception("Modification of rust allocated octrees not permitted!");
        }
    }

    [HarmonyPatch(typeof(Octree), nameof(Octree.UnloadChildren))]
    [HarmonyPrefix]
    public static bool Prefix(Octree __instance) {
        return __instance.data.m_AllocatorLabel != Allocator.RustSharedOctree;
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
    public static void Prefix(BatchOctrees __instance) {
        __instance.allocator = RustSharedOctreesAllocator.SharedOctreesAllocator;
    }
}


internal sealed class RustSharedOctreesAllocator() : SplitNativeArrayPool<byte>(0, 0, 0, 0, 0, 0, 0, 0) {

    internal static readonly RustSharedOctreesAllocator SharedOctreesAllocator = new();

    [HarmonyPatch(typeof(SplitNativeArrayPool<byte>), nameof(Get))]
    private static class RustAllocatorGetImpl {
        private static bool Prefix(
            SplitNativeArrayPool<byte> __instance, int minLength, ref NativeArray<byte> __result
        ) {
            throw new Exception("Cannot allocate rust Native Array here!");
        }
    }

    [HarmonyPatch(typeof(SplitNativeArrayPool<byte>), nameof(Return))]
    private static class RustAllocatorReturnImpl {
        private static bool Prefix(
            SplitNativeArrayPool<byte> __instance, ref NativeArray<byte> arr
        ) {
            if (__instance is not RustSharedOctreesAllocator) return true;
            arr.Dispose();
            return false;
        }
    }
}