using System.Collections.Generic;
using SNCoreEditor.Input;
using SNCoreEditor.UI.HotBar;
using SNCoreEditor.UI.HotBar.Interfaces;
using UnityEngine;
namespace SNCoreEditor.CoreWorkspaceStuff.Buttons;


internal class UndoButtonToggleTwo : IHotBarToggleAction, IHotBarDefinitionProvider
{
    public HotBarButtonDefinition Definition() => new(
        "UndoButtonToggleTwo",
        Assets.CoreBundle.LoadAsset<Sprite>("UndoIcon"),
        [InputRegistration.CtrlModifier, InputRegistration.UndoBind],
        () => new UndoButtonToggleTwo());
    
    public List<string> incompatibleWith { get; } = ["UndoButtonToggle"];
    
    public void OnActivated()
    {
        Plugin.Logger.LogError("TOGGLE PRESSED!!!!!!");
    }

    public void OnDeactivated()
    {
        
    }
}
