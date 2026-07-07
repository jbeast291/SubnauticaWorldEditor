using System;
using System.Collections.Generic;
using SNCoreEditor.UI.HotBar;

namespace SNCoreEditor.UI.Workspace;


public static class WorkspaceRegistration
{
    private static readonly Dictionary<Type, WorkspaceDefinition> _workspaces = new();
    
    public static void Register<T>(WorkspaceDefinition definition) where T : IWorkspace, new()
    {
        if (_workspaces.ContainsKey(typeof(T)))
            throw new Exception($"Workspace already registered: {nameof (T)}");
        
        _workspaces.Add(typeof(T), definition);
    }

    public static List<WorkspaceDefinition> GetAllWorkspaces()
    {
        List<WorkspaceDefinition> workspaces = new();
        foreach (WorkspaceDefinition definition in _workspaces.Values)
        {
            workspaces.Add(definition);
        }
        return workspaces;
    }
}