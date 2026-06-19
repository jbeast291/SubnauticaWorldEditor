using UnityEngine;

namespace SNCoreEditor.UI.Theme.Definitions.Components;

//NOTE: id prefer this be an interface, but unity won't serialize it as anything but a class :sob:
public abstract class ComponentThemeDefinition : ScriptableObject
{
    public virtual bool TryAssignToComponent(GameObject gameObject, ThemeAssigner.GraphicType graphicType)
    {
        return false;
    }
}