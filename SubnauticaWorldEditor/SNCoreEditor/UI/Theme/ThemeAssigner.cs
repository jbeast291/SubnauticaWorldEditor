using System;
using System.Linq;
using SNCoreEditor.UI.Theme.Definitions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SNCoreEditor.UI.Theme;


public sealed class ThemeAssigner : MonoBehaviour
{
    private RegionThemeMarker _regionThemeMarker;
    [SerializeField] private string graphicKey;

    private Action onThemeChange;

    private void OnValidate() => DetermineGraphicKey();

    private void DetermineGraphicKey()
    {
        if (!string.IsNullOrEmpty(graphicKey)) return;

        MonoBehaviour[] monos = gameObject.GetComponents<MonoBehaviour>();
        //This isn't the most efficient but its editor only so whatever :/
        if (monos.FirstOrDefault(mono => mono is Button) != null) graphicKey = "Button";
        else if (monos.FirstOrDefault(mono => mono is TextMeshProUGUI) != null) graphicKey = "Text";
        else if (monos.FirstOrDefault(mono => mono is Scrollbar) != null) graphicKey = "Scrollbar";
    } 

    public void RegisterForOnChange(Action action) => onThemeChange += action;
    
    private void Start()
    {
        _regionThemeMarker = GetComponentInParent<RegionThemeMarker>();
        if (_regionThemeMarker == null) throw new Exception($"Failed to determine the RegionTheme for {gameObject.name}");
        AssignTheme(ThemeManager.GetActiveTheme());
    }

    internal void AssignTheme(EditorThemeDefinition editorTheme)
    {
        if (string.IsNullOrEmpty(graphicKey) || graphicKey == "Unknown") throw new Exception($"Graphic Key ({graphicKey}) is unknown on {gameObject.name}! If no theme is intended for this object remove the assigner!");

        RegionThemeMarker currentRegion = _regionThemeMarker;
        while (currentRegion != null)
        {
            RegionThemeDefinition regionTheme = ThemeManager.GetRegionTheme(currentRegion.RegionName);
            if (regionTheme != null && regionTheme.TryAssignToComponent(gameObject, graphicKey))
            {
                onThemeChange?.Invoke();
                return;
            }
            currentRegion = currentRegion.transform.parent?.GetComponentInParent<RegionThemeMarker>();
        }
        throw new Exception($"No Region Theme Definition could be found for '{gameObject.name}' within '{editorTheme.name}' could not be found!");
    }
}