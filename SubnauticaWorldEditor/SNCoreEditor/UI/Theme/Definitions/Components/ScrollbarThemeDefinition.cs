using System;
using UnityEngine;
using UnityEngine.UI;

namespace SNCoreEditor.UI.Theme.Definitions.Components;

internal sealed class ScrollbarThemeDefinition : ComponentThemeDefinition
{
    [SerializeField] private Sprite baseSprite;
    [SerializeField] private Sprite highlightedSprite;
    [SerializeField] private Sprite pressedSprite;
    [SerializeField] private Sprite selectedSprite;
    [SerializeField] private Sprite disabledSprite;
    
    [UninitializedContext]
    public override string GraphicKey() => "Scrollbar";
    
    public override void AssignToComponent(GameObject gameObject)
    {
        if (!gameObject.TryGetComponent(out Scrollbar scrollbar)) throw new Exception("Failed to get Scrollbar component while assigning theme!");
        
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
    }
}