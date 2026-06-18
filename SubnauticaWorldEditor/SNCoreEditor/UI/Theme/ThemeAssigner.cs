using System;
using System.Linq;
using SNCoreEditor.UI.Theme.Definitions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SNCoreEditor.UI.Theme;

public class ThemeAssigner : MonoBehaviour
{
    private RegionThemeMarker _regionThemeMarker; 
    [SerializeField] private GraphicType graphicType;

    private void OnValidate() => DetermineGraphicType();

    private void DetermineGraphicType()
    {
        if (graphicType != default) return;
        
        MonoBehaviour[] monos = gameObject.GetComponents<MonoBehaviour>();
        //This isn't the most efficient but its editor only so whatever :/
        if (monos.FirstOrDefault(mono => mono is Button) != null) graphicType = GraphicType.Button;
        else if (monos.FirstOrDefault(mono => mono is TextMeshProUGUI) != null) graphicType = GraphicType.Text;
    }

    private void Start()
    {
        _regionThemeMarker = GetComponentInParent<RegionThemeMarker>();
        if (_regionThemeMarker == null) throw new Exception($"Failed to determine the RegionTheme for {gameObject.name}");
        AssignTheme(ThemeManager.GetActiveTheme());
    }

    internal void AssignTheme(EditorThemeDefinition editorTheme)
    {
        if (graphicType == GraphicType.Unknown) throw new Exception($"Graphic Type ({graphicType}) is unknown on {gameObject.name}! If no theme is intended for this object remove the assigner!");

        RegionThemeMarker currentRegion = _regionThemeMarker;
        while (currentRegion != null)
        {
            RegionThemeDefinition regionTheme = ThemeManager.GetRegionTheme(currentRegion.RegionName);
            if(regionTheme != null && regionTheme.TryAssignToComponent(gameObject, graphicType)) 
                return;
            
            currentRegion = currentRegion.transform.parent?.GetComponentInParent<RegionThemeMarker>();
        }
        throw new Exception($"No Region Theme Definition could be found for '{gameObject.name}' within '{editorTheme.name}' could not be found!");
    }

    public enum GraphicType
    {
        Unknown,
        Button,
        Text,
        Icon
    }
}