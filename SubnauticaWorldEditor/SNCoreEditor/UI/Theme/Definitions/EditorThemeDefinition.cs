using UnityEngine;

namespace SNCoreEditor.UI.Theme.Definitions;

[CreateAssetMenu(fileName = "YourThemeNameHere", menuName = "SNEditor/Themes/Create Theme Definition")]
public class EditorThemeDefinition : ScriptableObject
{
    public ThemeRegionDefinition[] themeRegions;
}