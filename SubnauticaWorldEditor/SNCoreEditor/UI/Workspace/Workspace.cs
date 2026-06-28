using UnityEngine;

namespace SNCoreEditor.UI.Workspace;

public abstract class Workspace(Workspace.Mode workspaceMode)
{
    public Mode WorkspaceMode { get; private set; } = workspaceMode;
    private string WorkspaceName { get; }
    private Sprite WorkspaceIcon { get; }

    public abstract void OnEnableWorkspace();
    public abstract void OnDisableWorkspace();

    public enum Mode
    {
        Persistent,
        Exclusive
    }
}