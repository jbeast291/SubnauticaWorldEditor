using UnityEngine;
using UnityEngine.UI;

namespace SNCoreEditor.UI.Theme.Definitions.Components;

[CreateAssetMenu(fileName = "IconThemeDefinition", menuName = "SNEditor/Themes/Components/Icon Definition")]
internal sealed class IconThemeDefinition : ComponentThemeDefinition
{
    [Tooltip("Changes the color of the icon.")]
    [SerializeField] internal Color color  = Color.white;
    
    public override bool TryAssignToComponent(GameObject gameObject, ThemeAssigner.GraphicType graphicType)
    {
        if (graphicType != ThemeAssigner.GraphicType.Icon) return false;
        if (!gameObject.TryGetComponent(out Image image)) return false;
        image.color = color;
        return true;
    }
}