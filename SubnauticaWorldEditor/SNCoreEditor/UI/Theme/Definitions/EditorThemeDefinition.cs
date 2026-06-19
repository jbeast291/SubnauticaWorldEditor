using UnityEngine;
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

namespace SNCoreEditor.UI.Theme.Definitions;

[CreateAssetMenu(fileName = "EditorThemeName", menuName = "SNEditor/Themes/Editor Theme Definition")]
public sealed class EditorThemeDefinition : ScriptableObject
{
    [Tooltip("The name of the theme as a language key. Will be translated when shown to the player")]
    [SerializeField] internal string themeNameLanguageKey;
    [Tooltip("The region themes for this theme. A 'base' region should be defined with all possible components at minimum")]
    [SerializeField] internal RegionThemeDefinition[] regionThemes;
}


