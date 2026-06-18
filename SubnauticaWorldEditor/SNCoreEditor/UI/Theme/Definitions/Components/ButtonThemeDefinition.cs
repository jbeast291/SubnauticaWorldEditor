using System;
using UnityEngine;
using UnityEngine.UI;

namespace SNCoreEditor.UI.Theme.Definitions.Components;

[CreateAssetMenu(fileName = "ButtonThemeDefinition", menuName = "SNEditor/Themes/Components/Button Theme Definition")]
public class ButtonThemeDefinition : ComponentThemeDefinition
{
    [SerializeField] public Sprite baseSprite;
    [SerializeField] public Sprite highlightedSprite;
    [SerializeField] public Sprite pressedSprite;
    [SerializeField] public Sprite selectedSprite;
    [SerializeField] public Sprite disabledSprite;
    
    public override bool TryAssignToComponent(GameObject gameObject, ThemeAssigner.GraphicType graphicType)
    {
        if (graphicType != ThemeAssigner.GraphicType.Button) return false;
        if (!gameObject.TryGetComponent(out UnityEngine.UI.Button button)) return false;
        
        if (button.targetGraphic is not Image img)
        {
            throw new Exception("Cannot apply button theme to a button without an image target graphic");
        }
        img.sprite = baseSprite;
        SpriteState state = new()
        {
            highlightedSprite = highlightedSprite,
            pressedSprite = pressedSprite,
            selectedSprite = selectedSprite,
            disabledSprite = disabledSprite
        };
        button.spriteState = state;
        return true;
    }
}