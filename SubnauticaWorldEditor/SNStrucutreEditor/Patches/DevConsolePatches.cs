using HarmonyLib;
using SNStructureEditor.UI;

namespace SNStructureEditor.Patches;

[HarmonyPatch(typeof(DevConsole))]
public static class DevConsolePatches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(DevConsole.SetState))]
    public static bool SetStatePrefix(DevConsole __instance, bool value)
    {
        var entityWindow = UIEntityWindow.Main;
        return entityWindow == null || !entityWindow.isActiveAndEnabled;
    }
}