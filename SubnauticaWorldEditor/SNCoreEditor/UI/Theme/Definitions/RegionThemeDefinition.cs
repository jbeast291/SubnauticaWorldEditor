using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SNCoreEditor.UI.Theme.Definitions;

[CreateAssetMenu(fileName = "RegionThemeDefinition", menuName = "SNEditor/Themes/Region Theme Definition")]
public class RegionThemeDefinition : ScriptableObject
{
    [SerializeField] public string regionName;
    [SerializeField] public ComponentThemeDefinition[] componentsThemeDefinitions;

    private void OnValidate()
    {
        regionName = name;
    }

    public virtual bool TryAssignToComponent(GameObject gameObject, ThemeAssigner.GraphicType graphicType)
    {
        foreach (ComponentThemeDefinition assigner in componentsThemeDefinitions)
        {
            if(assigner.TryAssignToComponent(gameObject, graphicType)) return true;
        }
        return false;
    }
}