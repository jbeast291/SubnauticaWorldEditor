using UnityEngine;
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

namespace SNCoreEditor.UI.Theme.Definitions;

[CreateAssetMenu(fileName = "EditorThemeName", menuName = "SNEditor/Themes/Editor Theme Definition")]
public sealed class EditorThemeDefinition : ScriptableObject
{
    [SerializeField] internal string themeNameLanguageKey;
    [SerializeField] internal RegionThemeDefinition[] regionThemes;
}


