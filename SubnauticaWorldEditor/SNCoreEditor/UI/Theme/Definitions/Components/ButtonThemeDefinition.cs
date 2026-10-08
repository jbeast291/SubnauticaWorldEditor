using System;
using UnityEngine;
using UnityEngine.UI;

namespace SNCoreEditor.UI.Theme.Definitions.Components;

internal sealed class ButtonThemeDefinition : ComponentThemeDefinition
{
    [SerializeField] private Sprite baseSprite;
    [SerializeField] private Sprite highlightedSprite;
    [SerializeField] private Sprite pressedSprite;
    [SerializeField] private Sprite selectedSprite;
    [SerializeField] private Sprite disabledSprite;

    public override string GraphicKey() => "Button";
    
    public override void AssignToComponent(GameObject gameObject)
    {
        if (!gameObject.TryGetComponent(out Button button)) throw new Exception("Cannot apply button theme to a button without a button...");
        
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
    }
}