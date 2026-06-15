using HarmonyLib;
using SNStructureEditor.Mono;
using UnityEngine;

namespace SNStructureEditor.Patches;

[HarmonyPatch(typeof(Player))]
public static class PlayerPatches
{
    [HarmonyPatch(nameof(Player.Start))]
    [HarmonyPostfix]
    public static void StartPostfix()
    {
        var inputHandlerObj = new GameObject("StructureHelperInputHandler");
        inputHandlerObj.AddComponent<InputHandler>();
    }
}