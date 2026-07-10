using SNCoreEditor.UI.Workspace;
using UnityEngine;
namespace SNCoreEditor.CoreWorkspaceStuff;

internal class CoreWorkspace : IWorkspace
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
        
    }
}