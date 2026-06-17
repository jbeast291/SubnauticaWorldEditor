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
        GameObject inputHandlerObj = new GameObject("WorldEditorInputHandler");
        inputHandlerObj.AddComponent<InputHandler>();
    }
}