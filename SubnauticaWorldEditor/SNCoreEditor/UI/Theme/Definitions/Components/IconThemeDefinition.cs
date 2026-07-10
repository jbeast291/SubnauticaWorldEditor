using System;
using UnityEngine;
using UnityEngine.UI;

namespace SNCoreEditor.UI.Theme.Definitions.Components;

internal sealed class IconThemeDefinition : ComponentThemeDefinition
{
    [Tooltip("Changes the color of the icon.")]
    [SerializeField] private Color color  = Color.white;
    
    [UninitializedContext]
    public override string GraphicKey() => "Icon";
    
    public override void AssignToComponent(GameObject gameObject)
    {
        if (!gameObject.TryGetComponent(out Image image)) throw new Exception("Failed to get image on image graphic!");
        image.color = color;
    }
}