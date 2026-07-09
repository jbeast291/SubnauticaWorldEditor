using System.Collections.Generic;
using SNCoreEditor.Input;
using SNCoreEditor.UI.HotBar;
using SNCoreEditor.UI.HotBar.Interfaces;
using UnityEngine;
namespace SNCoreEditor.CoreWorkspaceStuff.Buttons;


public class UndoButtonToggle : IHotBarToggleAction, IHotBarDefinitionProvider
{
    public HotBarButtonDefinition Definition() => new(
        Assets.CoreBundle.LoadAsset<Sprite>("UndoIcon"),
        "UndoButtonToggle",
        [InputRegistration.AltToolHotkeyModifier, InputRegistration.UndoBind],
        () => new UndoButtonToggle());
    
    public List<string> incompatibleWith { get; } = new();
    
    public void OnActivated()
    {
        Plugin.Logger.LogError("TOGGLE PRESSED!!!!!!");
    }

    public void OnDeactivated()
    {
        
    }
}
