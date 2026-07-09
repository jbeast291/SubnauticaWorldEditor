using System;
using UnityEngine;
using UnityEngine.UI;

namespace SNCoreEditor.UI.Theme.Definitions.Components;

[CreateAssetMenu(fileName = "ButtonThemeDefinition", menuName = "SNEditor/Themes/Components/Button Definition")]
internal sealed class ButtonThemeDefinition : ComponentThemeDefinition
{
    [SerializeField] internal Sprite baseSprite;
    [SerializeField] internal Sprite highlightedSprite;
    [SerializeField] internal Sprite pressedSprite;
    [SerializeField] internal Sprite selectedSprite;
    [SerializeField] internal Sprite disabledSprite;

    [UninitializedContext]
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