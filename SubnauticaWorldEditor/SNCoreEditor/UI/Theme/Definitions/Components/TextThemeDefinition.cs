using System;
using TMPro;
using UnityEngine;

namespace SNCoreEditor.UI.Theme.Definitions.Components;

[GraphicKey("Text")]
[CreateAssetMenu(fileName = "TextThemeDefinition", menuName = "SNEditor/Themes/Components/Text Definition")]
internal sealed class TextThemeDefinition : ComponentThemeDefinition
{
    [Tooltip("Changes the color of the text")]
    [SerializeField] internal Color color = Color.white;
    
    public override void AssignToComponent(GameObject gameObject)
    {
        if (!gameObject.TryGetComponent(out TextMeshProUGUI text)) throw new Exception("Failed to get TextMeshProUGUI component while assigning theme!");
        text.color = color;
    }
}