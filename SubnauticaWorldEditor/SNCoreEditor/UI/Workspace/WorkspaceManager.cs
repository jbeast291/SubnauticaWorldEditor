using System;
using System.Collections.Generic;
using UnityEngine;
namespace SNCoreEditor.UI.Workspace;


public class WorkspaceManager : MonoBehaviour
{
    private Dictionary<WorkspaceDefinition, IWorkspace> Workspaces;
    
    private void Start()
    {
        foreach (WorkspaceDefinition workspaceDef in WorkspaceRegistration.GetAllWorkspaces())
        {
            IWorkspace workspace = workspaceDef.WorkspaceFactory.Invoke();
            Workspaces.Add(workspaceDef, workspace);
        }
    }
}
