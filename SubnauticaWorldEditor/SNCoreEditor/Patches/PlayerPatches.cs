using HarmonyLib;
using SNCoreEditor.Input;
using UnityEngine;

namespace SNCoreEditor.Patches;

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