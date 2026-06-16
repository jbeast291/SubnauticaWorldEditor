using System.Collections.Generic;

namespace SNCoreEditor.UI.Workspace;

public class WorkspaceManager
{
    public List<IWorkspace> Workspaces { get; }

    public void RegisterWorkspace(IWorkspace workspace) => Workspaces.Add(workspace);
}