using System;
using System.Collections.Generic;
using HarmonyLib;
using SNTerrainEditor;
using TerrainEditor.Core.DataTypes;
using WorldStreaming;

namespace TerrainEditor.Patches;

[HarmonyPatch(typeof(BatchOctrees))]
public class BatchOctreesPatcher
{
    private static readonly List<IBatchStreamingEventListener> _listeners = new();

    public static void RegisterListener(IBatchStreamingEventListener listener) => _listeners.Add(listener);
    public static void DeregisterListener(IBatchStreamingEventListener listener) => _listeners.Remove(listener);

    [HarmonyPatch(nameof(BatchOctrees.OnLoaded))]
    [HarmonyPrefix]
    private static void OnBatchLoaded_Prefix(BatchOctrees __instance)
    {
        Plugin.Logger.LogError($"LOADED {__instance.id}");
        _listeners.ForEach(listener => listener.OnBatchLoaded(__instance));
    }

    [HarmonyPatch(nameof(BatchOctrees.BeginUnloadOctrees), [])]
    [HarmonyPrefix]
    private static void BeginUnloadOctrees_Prefix(BatchOctrees __instance)
    {
        Plugin.Logger.LogError($"UNLOADED {__instance.id}");
        _listeners.ForEach(listener => listener.OnBatchUnloaded(__instance));
    }

    /// <summary>
    /// When batches become managed, we return the native arrays back to the allocator pool.
    /// As a result, when a batch is cleared but also managed, we must ensure that our native arrays don't get added to that pool.
    /// </summary>
    [HarmonyPatch(nameof(BatchOctrees.ClearOctrees), [])]
    [HarmonyPrefix]
    private static void ClearOctrees_Prefix(BatchOctrees __instance)
        => _listeners.ForEach(listener => listener.OnEarlyClearOctrees(__instance));
}

public interface IBatchStreamingEventListener
{
    void OnBatchLoaded(BatchOctrees batchOctrees);
    void OnBatchUnloaded(BatchOctrees batchOctrees);
    void OnEarlyClearOctrees(BatchOctrees batchOctrees);
}

/*
[HarmonyPatch(typeof(BatchOctrees))]
internal static class BatchOctreesPatches
{
    [HarmonyPatch(nameof(BatchOctrees.LoadOctrees))]
    [HarmonyPrefix]
    private static bool LoadOctreesPreFix(ref BatchOctrees __instance, ref bool __result)
    {
        if (EditorBatchManager.main.editorManagedBatchIds.Contains(__instance.id)) return true;
        //custom initial octree loading here!

        __result = false;
        return true;
    }
}
*/
/*
[HarmonyPatch]
internal static class DebugBlockyPatch
{
    static MethodBase TargetMethod()
    {
        return typeof(MeshBuilder)
            .GetInterfaceMap(typeof(IVoxeland))
            .TargetMethods
            .First(m => m.Name.Contains("debugBlocky"));
    }

    static bool Prefix(ref bool __result)
    {
        __result = true;
        return false;
    }
}*/