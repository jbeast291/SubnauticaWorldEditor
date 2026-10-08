using System;
using UnityEngine;
using UnityEngine.UI;
namespace SNCoreEditor.UI.Theme.Definitions.Components;


public class BackgroundThemeDefinition : ComponentThemeDefinition
{
    [SerializeField] private Sprite backgroundSprite;
    
    public override string GraphicKey() => "BackgroundSprite";

    public override void AssignToComponent(GameObject gameObject)
    {
        if (!gameObject.TryGetComponent(out Image image)) throw new Exception("Failed to get image on image graphic!");
        image.sprite = backgroundSprite;
    }
}
