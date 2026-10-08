using SNCoreEditor.Input;
using SNCoreEditor.UI.HotBar;
using SNCoreEditor.UI.HotBar.Interfaces;
using UnityEngine;

namespace SNTerrainEditor.Workspace.Button;

internal class RemoveTool : ICursorTool, ICursorToolDefinitionProvider
{
    public CursorToolDefinition Definition() => new(
        "RemoveTool",
        Assets.TerrainBundle.LoadAsset<Sprite>("Checkmark"),
        [CoreInput.BrushDecreaseScale],
        () => new AddTool());
    
    public void OnActivated()
    {
        Plugin.LogError("Remove Enabled");
    }

    public void OnDeactivated()
    {
        Plugin.LogError("Remove Disabled");
    }
}