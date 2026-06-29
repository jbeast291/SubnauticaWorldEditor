using SNCoreEditor.UI.Theme.Definitions.Components;
using UnityEngine;

namespace SNCoreEditor.UI.Theme.Definitions;

[CreateAssetMenu(fileName = "RegionThemeDefinition", menuName = "SNEditor/Themes/Region Theme Definition")]
public sealed class RegionThemeDefinition : ScriptableObject
{
    [Tooltip("Name of the region. Should be defined somewhere in a 'RegionThemeMarker' assigners to pull from this region. Automatically set to the name of the scriptable object")]
    [SerializeField] internal string regionName;
    [Tooltip("Component definitions for this region. If no component here has a theme for a graphicType, the assigner will advance upwards to the next region")]
    [SerializeField] internal ComponentThemeDefinition[] componentsThemeDefinitions;

    private void OnValidate()
    {
        regionName = name;
    }
    
    public void SetRegionName(string name) => this.regionName = name;

    internal bool TryAssignToComponent(GameObject gameObject, ThemeAssigner.GraphicType graphicType)
    {
        foreach (ComponentThemeDefinition assigner in componentsThemeDefinitions)
        {
            if(assigner.TryAssignToComponent(gameObject, graphicType)) return true;
        }
        return false;
    }
}