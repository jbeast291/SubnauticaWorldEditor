using System;
using SNCoreEditor.UI.Theme.Definitions.Components;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SNCoreEditor.UI.Theme.Definitions;

[CreateAssetMenu(fileName = "HotBarTheme", menuName = "SNEditor/Themes/Regions/Create HotBar Theme")]
public class HotBarRegionThemeDefinition : ThemeRegionDefinition, IRegionThemeAssigner
{
    private void OnValidate() { regionName = "HotBar"; }
    public ButtonTheme buttonTheme;
    public Color textAndIconColor = Color.white;

    public void AssignToButton(Button button)
    {
        buttonTheme.AssignToButton(button);
    }

    public void AssignToBackgroundImage(Image img) => throw new Exception("Hot Bar theme does not define background images");

    public void AssignToIcon(Image img) => img.color = textAndIconColor;

    public void AssignToText(TextMeshProUGUI text) => text.color = textAndIconColor;
}
