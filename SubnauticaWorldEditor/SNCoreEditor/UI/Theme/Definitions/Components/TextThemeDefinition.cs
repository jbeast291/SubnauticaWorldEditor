using TMPro;
using UnityEngine;

namespace SNCoreEditor.UI.Theme.Definitions.Components;

[CreateAssetMenu(fileName = "TextThemeDefinition", menuName = "SNEditor/Themes/Components/Text Definition")]
internal sealed class TextThemeDefinition : ComponentThemeDefinition
{
    [Tooltip("Changes the color of the text")]
    [SerializeField] internal Color color = Color.white;
    
    public override bool TryAssignToComponent(GameObject gameObject, ThemeAssigner.GraphicType graphicType)
    {
        if (graphicType != ThemeAssigner.GraphicType.Text) return false;
        if (!gameObject.TryGetComponent(out TextMeshProUGUI text)) return false;
        text.color = color;
        return true;
    }
}