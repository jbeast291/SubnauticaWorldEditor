using UnityEngine;

namespace SNCoreEditor.UI.Workspace;

public interface IWorkspace
{
    string WorkspaceName { get; }
    Sprite WorkspaceIcon { get; }
    
    void OnEnableWorkspace();
    void OnDisableWorkspace();
}