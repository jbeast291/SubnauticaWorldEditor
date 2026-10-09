using System;
using System.Collections.Generic;
using UnityEngine;
namespace SNCoreEditor.UI.Workspace;


public class WorkspaceManager : MonoBehaviour
{
    private ICoreWorkspace _coreWorkspace;
    
    private readonly Dictionary<WorkspaceDefinition, IWorkspace> _exclusiveWorkspaces = new();
    private IWorkspace _activeExclusive;
    
    private void Start()
    {
        InitWorkspaces();
    }

    private void InitWorkspaces()
    {
        _coreWorkspace = (ICoreWorkspace) WorkspaceRegistration.GetCoreWorkspace().WorkspaceFactory.Invoke();
        _coreWorkspace.Initialize();
        
        foreach (WorkspaceDefinition workspaceDef in WorkspaceRegistration.GetExclusiveWorkspaces())
        {
            IWorkspace workspace = workspaceDef.WorkspaceFactory.Invoke();
            _exclusiveWorkspaces.Add(workspaceDef, workspace);
            workspace.Initialize();
        }
    }

    private void SwitchWorkspace(WorkspaceDefinition workspace)
    {
        if(!_exclusiveWorkspaces.TryGetValue(workspace, out IWorkspace newWorkspace)) {
            throw new Exception($"Workspace not initialized: {workspace}");
        }
        _activeExclusive.OnDisableWorkspace();
        _activeExclusive = newWorkspace;
        newWorkspace.OnEnableWorkspace();
    }
}
