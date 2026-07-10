using System;
using System.Collections.Generic;
using System.Linq;
using Nautilus.Handlers;
using SNCoreEditor.UI.Theme.Definitions;
using UnityEngine;

namespace SNCoreEditor.UI.Theme;

public sealed class ThemeManager : MonoBehaviour
{
    private static ThemeManager _instance;
    
    private static readonly HashSet<EditorThemeDefinition> Themes = new();
    private static EditorThemeDefinition _activeTheme;

    private static bool CoreThemesRegistered;

    private void Awake()
    {
        if (_instance != null)
        {
            DestroyImmediate(this);
            throw new Exception("Duplicate ThemeManager detected!");
        }
        _instance = this;
    }
    
    internal static void Initialize(WaitScreenHandler.WaitScreenTask task)
    {
        if(CoreThemesRegistered) return;
        LoadCoreThemes();
        SetActiveTheme("SubnauticaTheme");
        CoreThemesRegistered = true;
    }

    private static void LoadCoreThemes()
    {
        string[] coreThemeAssetNames = ["SubnauticaTheme", "DarkTheme"];
        foreach (string coreThemeName in coreThemeAssetNames)
        {
            EditorThemeDefinition editorThemeDefinition = Assets.CoreBundle.LoadAsset<EditorThemeDefinition>(coreThemeName);
            RegisterEditorTheme(editorThemeDefinition);
        }
    }
    
    public static void RegisterEditorTheme(EditorThemeDefinition theme)
    {
        Themes.Add(theme);
    }

    internal static void SetActiveTheme(string name, bool refresh = false)
    {
        EditorThemeDefinition theme = Themes.FirstOrDefault(t => t.name == name);
        if (theme == null)
        {
            throw new Exception("Invalid theme name attempted to active! Falling back to previous (if it exists)");
        }
        _activeTheme = theme;
        if (refresh) _instance?.RefreshThemeAssigners();
    }

    internal static RegionThemeDefinition GetRegionTheme(string regionName)
    {
        return _activeTheme.regionThemes.FirstOrDefault(definition => definition.regionName == regionName);
    }

    internal void RefreshThemeAssigners()
    {
        ThemeAssigner[] assigners = GetComponentsInChildren<ThemeAssigner>(includeInactive: true);
        foreach (ThemeAssigner assigner in assigners)
        {
            assigner.AssignTheme(_activeTheme);
        }
    }

    public static EditorThemeDefinition GetActiveTheme() => _activeTheme;
}