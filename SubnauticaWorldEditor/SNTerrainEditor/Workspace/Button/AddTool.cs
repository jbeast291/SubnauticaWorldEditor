using SNCoreEditor.Input;
using SNCoreEditor.UI.HotBar;
using SNCoreEditor.UI.HotBar.Interfaces;
using SNTerrainEditor.Core;
using UnityEngine;
namespace SNTerrainEditor.Workspace.Button;


internal class AddTool : ICursorTool, ICursorToolDefinitionProvider
{
    public CursorToolDefinition Definition() => new(
        "AddTool",
        Assets.TerrainBundle.LoadAsset<Sprite>("Checkmark"),
        [CoreInput.UndoBind],
        () => new AddTool());
    
    public void OnActivated()
    {
        Plugin.LogError("Add Enabled");
        TEBatchManager.main.DEBUG__Batch121812Modify();
    }

    public void OnDeactivated()
    {
        Plugin.LogError("Add Disabled");
    }
}
