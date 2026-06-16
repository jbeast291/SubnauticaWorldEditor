using HarmonyLib;
using SNCoreEditor.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SNCoreEditor.Patches;

[HarmonyPatch(typeof(IngameMenu))]
public static class IngameMenuPatches
{
    [HarmonyPatch(nameof(IngameMenu.Awake))]
    [HarmonyPostfix]
    public static void AwakePostfix(IngameMenu __instance)
    {
        Transform buttonLayout = __instance.transform.Find("Main/ButtonLayout");
        Transform structuresButton = Object.Instantiate(buttonLayout.transform.Find("ButtonBack"), buttonLayout.GetComponent<RectTransform>());
        
        structuresButton.transform.SetSiblingIndex(structuresButton.parent.Find("ButtonSave").GetSiblingIndex() + 1);
        structuresButton.GetComponentInChildren<TextMeshProUGUI>().text = $"SN World Editor";
        structuresButton.name = "ButtonSNWorldEditor";
        
        Button button = structuresButton.GetComponent<Button>();
        button.onClick = new();
        button.onClick.AddListener(CoreEditorCanvasManager.main.ToggleEditorVisibility);
    }
}