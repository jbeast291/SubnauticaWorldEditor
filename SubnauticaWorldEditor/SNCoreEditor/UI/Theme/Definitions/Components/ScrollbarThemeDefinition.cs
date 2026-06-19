using System;
using UnityEngine;
using UnityEngine.UI;

namespace SNCoreEditor.UI.Theme.Definitions.Components;

[CreateAssetMenu(fileName = "ScrollbarThemeDefinition", menuName = "SNEditor/Themes/Components/ScrollBar Definition")]
internal sealed  class ScrollbarThemeDefinition : ComponentThemeDefinition
{
    [SerializeField] internal Sprite baseSprite;
    [SerializeField] internal Sprite highlightedSprite;
    [SerializeField] internal Sprite pressedSprite;
    [SerializeField] internal Sprite selectedSprite;
    [SerializeField] internal Sprite disabledSprite;
    
    public override bool TryAssignToComponent(GameObject gameObject, ThemeAssigner.GraphicType graphicType)
    {
        if (graphicType != ThemeAssigner.GraphicType.Scrollbar) return false;
        if (!gameObject.TryGetComponent(out Scrollbar scrollbar)) return false;
        
        if (scrollbar.targetGraphic is not Image img)
        {
            throw new Exception("Cannot apply scrollbar theme to a scrollbar without an image target graphic");
        }
        img.sprite = baseSprite;
        SpriteState state = new()
        {
            highlightedSprite = highlightedSprite,
            pressedSprite = pressedSprite,
            selectedSprite = selectedSprite,
            disabledSprite = disabledSprite
        };
        scrollbar.spriteState = state;
        return true;
    }
}