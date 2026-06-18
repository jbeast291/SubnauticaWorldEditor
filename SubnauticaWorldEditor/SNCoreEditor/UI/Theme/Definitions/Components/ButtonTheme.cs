using System;
using UnityEngine;
using UnityEngine.UI;

namespace SNCoreEditor.UI.Theme.Definitions.Components;

[CreateAssetMenu(fileName = "ButtonTheme", menuName = "SNEditor/Themes/Regions/Components/Button")]
public class ButtonTheme : ScriptableObject
{
    public Sprite baseSprite;
    public Sprite highlightedSprite;
    public Sprite pressedSprite;
    public Sprite selectedSprite;
    public Sprite disabledSprite;
    
    public void AssignToButton(Button button)
    {
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