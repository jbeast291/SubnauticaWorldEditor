using System.Text.RegularExpressions;
using SNCoreEditor.UI.Theme;
using SNCoreEditor.UI.Theme.Definitions.Components;
using TMPro;
using UnityEngine;
namespace SNCoreEditor.UI.HotBar;


public class HotBarInputGlyphText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI ButtonHoverText;
    [SerializeField] private ThemeAssigner themeAssigner;
    
    internal HotBarButtonDefinition definition { private get; set; }
    
    private void Awake() => themeAssigner.RegisterForOnChange(OnThemeChange);
    
    private void OnThemeChange(ComponentThemeDefinition themeDefinition)
    {
        TextThemeDefinition textDefinition = themeDefinition as TextThemeDefinition;
        string text = GenerateHotKeyText(definition);
        ButtonHoverText.text = SetGlyphColors(text, textDefinition);
    }
    
    private static string GenerateHotKeyText(HotBarButtonDefinition definition)
    {
        string text = "";
        for (int i = 0; i < definition.buttons.Count; i++)
        {
            text += GameInput.FormatButton(definition.buttons[i]);
            
            if (i < definition.buttons.Count - 1) text += " + ";
        }
        return text;
    }

    private static string SetGlyphColors(string text, TextThemeDefinition textDefinition)
    {
        string newColorHex = ColorUtility.ToHtmlStringRGBA(textDefinition.GetInputGlyphColor());

        return Regex.Replace(
            text,
            @"color=#?[0-9A-Fa-f]{6,8}",
            $"color=#{newColorHex}");
    }
}
