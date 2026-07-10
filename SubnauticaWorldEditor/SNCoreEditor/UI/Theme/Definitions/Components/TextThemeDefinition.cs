using System;
using TMPro;
using UnityEngine;

namespace SNCoreEditor.UI.Theme.Definitions.Components;

internal sealed class TextThemeDefinition : ComponentThemeDefinition
{
    [Tooltip("Changes the color of the text")]
    [SerializeField] internal Color color = Color.white;
    [Tooltip("Changes the color of input glyph icons/text. If this region does not have input glyphs this can be ignored")]
    [SerializeField] internal Color inputGlyphColor = new Color32(173, 248, 255, 255);
    
    [UninitializedContext]
    public override string GraphicKey() => "Text";
    
    public override void AssignToComponent(GameObject gameObject)
    {
        if (!gameObject.TryGetComponent(out TextMeshProUGUI text)) throw new Exception("Failed to get TextMeshProUGUI component while assigning theme!");
        text.color = color;
    }
}