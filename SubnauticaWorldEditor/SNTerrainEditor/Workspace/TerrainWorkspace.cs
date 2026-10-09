using SNCoreEditor.UI.Workspace;
namespace SNTerrainEditor.Workspace;


public class TerrainWorkspace : IWorkspace
{
    public void Initialize()
    {
        Plugin.LogError("TerrainWorkspace initialized");
    }
    public void OnEnableWorkspace()
    {
        Plugin.LogError("TerrainWorkspace Enabled");
    }
    public void OnDisableWorkspace()
    {
        Plugin.LogError("TerrainWorkspace Disabled");
    }
}
