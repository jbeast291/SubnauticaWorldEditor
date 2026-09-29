using SNCoreEditor.Input;
using SNCoreEditor.UI.HotBar;
using SNCoreEditor.UI.HotBar.Interfaces;
using SNTerrainEditor.Core;
using UnityEngine;
namespace SNTerrainEditor.Workspace.Button;


internal class CheckMarkButton : IHotBarAction, IHotBarDefinitionProvider
{
    public HotBarButtonDefinition Definition() => new(
        "CheckMark",
        Assets.TerrainBundle.LoadAsset<Sprite>("Checkmark"), 
        [InputRegistration.UndoBind],
        () => new CheckMarkButton());
    
    public void OnActivated()
    {
        Plugin.LogError("ACTION PRESSED!!!!!!");
        EditorBatchManager.main.DEBUG__Batch121812Modify();
    }
}
