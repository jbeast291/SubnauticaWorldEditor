using TMPro;
using UnityEngine.UI;

namespace SNCoreEditor.UI.Theme;

public interface IRegionThemeAssigner
{
    /// <summary>
    /// Assigns the theme to a button
    /// </summary>
    void AssignToButton(Button button);
    
    /// <summary>
    /// Assigns the theme to an image
    /// </summary>
    void AssignToBackgroundImage(Image img);

    /// <summary>
    /// Assigns the theme to an icon
    /// </summary>
    void AssignToIcon(Image img);
    
    /// <summary>
    /// Assigns the theme to text
    /// </summary>
    void AssignToText(TextMeshProUGUI text);
}