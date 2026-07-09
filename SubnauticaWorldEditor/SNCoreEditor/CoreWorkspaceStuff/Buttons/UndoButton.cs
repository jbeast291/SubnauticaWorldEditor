using SNCoreEditor.Input;
using SNCoreEditor.UI.HotBar;
using SNCoreEditor.UI.HotBar.Interfaces;
using UnityEngine;
namespace SNCoreEditor.CoreWorkspaceStuff.Buttons;


public class UndoButton : IHotBarAction, IHotBarDefinitionProvider
{
    public HotBarButtonDefinition Definition() => new(
        "UndoButton",
        Assets.CoreBundle.LoadAsset<Sprite>("UndoIcon"), 
        [InputRegistration.UndoBind],
        () => new UndoButton());
    
    public void OnActivated()
    {
        Plugin.Logger.LogError("ACTION PRESSED!!!!!!");
    }
}
