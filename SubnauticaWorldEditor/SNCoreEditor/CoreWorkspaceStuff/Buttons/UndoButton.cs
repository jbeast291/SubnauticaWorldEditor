using SNCoreEditor.UI;
using SNCoreEditor.UI.HotBar;
namespace SNCoreEditor.CoreWorkspaceStuff.Buttons;


public class UndoButton : IHotBarButton
{
    public void OnActivated()
    {
        Plugin.Logger.LogError("BUTTON PRESSED!!!!!!");
    }
}
