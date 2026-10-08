using UnityEngine;

namespace SNCoreEditor.UI.Theme.Definitions.Components;

//NOTE: id prefer this be an interface, but unity won't serialize it as anything but a class :sob:
public abstract class ComponentThemeDefinition : ScriptableObject
{
    public abstract string GraphicKey();
    
    public abstract void AssignToComponent(GameObject gameObject);
}