using SNCoreEditor.UI.Workspace;
using UnityEngine;
namespace SNCoreEditor.CoreWorkspaceStuff;

internal class CoreWorkspace : ICoreWorkspace
{
    public void Initialize()
    {
        Plugin.Logger.LogError("Initialized CoreWorkspace");
    }
    
    public void OnEnableWorkspace()
    {
        Plugin.Logger.LogError("Enabled CoreWorkspace");
    }

    public void OnDisableWorkspace()
    {
        Plugin.Logger.LogError("Disabled CoreWorkspace");
    }
}