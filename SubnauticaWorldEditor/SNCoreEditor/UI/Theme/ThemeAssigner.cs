using System;
using System.Linq;
using SNCoreEditor.UI.Theme.Definitions;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace SNCoreEditor.UI.Theme;

public class ThemeAssigner : MonoBehaviour
{
    private RegionTheme _regionTheme; 
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
        _regionTheme = GetComponentInParent<RegionTheme>();
        if (_regionTheme == null) throw new Exception($"Failed to determine the RegionTheme for {gameObject.name}");
        AssignTheme();
    }

    internal void AssignTheme()
    {
        if (graphicType == GraphicType.Unknown) return;
        
        EditorThemeDefinition themeDefinition = ThemeManager.GetActiveTheme();
        ThemeRegionDefinition regionDefinition = themeDefinition.themeRegions.FirstOrDefault(definition => definition.regionName == _regionTheme.RegionName);
        if (regionDefinition == null)
        {
            throw new Exception($"Region Definition could not be found for {_regionTheme.RegionName}");
        }

        if (regionDefinition is not IRegionThemeAssigner themeAssigner)
        {
            throw new Exception($"Region Definition ({regionDefinition.regionName}) within ({themeDefinition.name}) does not define a IThemeAssigner to apply the theme!");
        }
        
        switch (graphicType)
        {
            case GraphicType.Button:
                themeAssigner.AssignToButton(gameObject.GetComponent<Button>());
                break;
            case GraphicType.Text:
                themeAssigner.AssignToText(gameObject.GetComponent<TextMeshProUGUI>());
                break;
            case GraphicType.Icon:
                themeAssigner.AssignToIcon(gameObject.GetComponent<Image>());
                break;
            case GraphicType.BackgroundImage:
                themeAssigner.AssignToBackgroundImage(gameObject.GetComponent<Image>());
                break;
        }
    }

    private enum GraphicType
    {
        Unknown,
        Button,
        Text,
        Icon,
        BackgroundImage
    }
}