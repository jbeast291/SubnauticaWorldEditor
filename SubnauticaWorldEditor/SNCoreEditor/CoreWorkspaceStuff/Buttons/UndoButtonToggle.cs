using System.Collections.Generic;
using SNCoreEditor.Input;
using SNCoreEditor.UI.HotBar;
using SNCoreEditor.UI.HotBar.Interfaces;
using UnityEngine;
namespace SNCoreEditor.CoreWorkspaceStuff.Buttons;


public class UndoButtonToggle : IHotBarToggleAction, IHotBarDefinitionProvider
{
    public HotBarButtonDefinition Definition() => new(
        "UndoButtonToggle",
        Assets.CoreBundle.LoadAsset<Sprite>("UndoIcon"),
        [InputRegistration.CtrlModifier, InputRegistration.UndoBind],
        () => new UndoButtonToggle());
    
    public List<string> incompatibleWith { get; } = ["UndoButtonToggleTwo"];
    
    public void OnActivated()
    {
        Plugin.Logger.LogError("TOGGLE PRESSED!!!!!!");
    }

    public void OnDeactivated()
    {
        
    }
    
    public void OnUpdate() { }
}
