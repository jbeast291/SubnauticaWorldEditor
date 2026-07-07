using System;
using System.Collections.Generic;
using System.Reflection;
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

    internal bool TryAssignToComponent(GameObject gameObject, string graphicKey)
    {
        foreach (ComponentThemeDefinition assigner in componentsThemeDefinitions)
        {
            Type componentType = assigner.GetType();
            if (!componentKeys.TryGetValue(componentType, out string key))
            {
                key = GetGraphicKeyFromAttribute(componentType);
                componentKeys.Add(componentType, key);
            }
            if (key != graphicKey) continue;
            
            assigner.AssignToComponent(gameObject);
            return true;
        }
        return false;
    }

    private static string GetGraphicKeyFromAttribute(Type componentType)
    {
        GraphicKeyAttribute attribute = componentType.GetCustomAttribute<GraphicKeyAttribute>();

        if (attribute == null) throw new Exception($"{componentType.Name} has no GraphicKeyAttribute! A component definition MUST have this attribute");
        
        return attribute.Key;
    }
    
    private static readonly Dictionary<Type, string> componentKeys = new();
}