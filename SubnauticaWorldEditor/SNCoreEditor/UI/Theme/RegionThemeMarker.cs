using UnityEngine;

namespace SNCoreEditor.UI.Theme;

/// <summary>
/// This component marks a game object and all its children as part of a theme for a region
/// </summary>
public sealed class RegionThemeMarker : MonoBehaviour
{
    [SerializeField] private string regionName = "Base";
    public string RegionName => regionName;
}