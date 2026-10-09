using System;
using System.Collections;
using System.Collections.Generic;
using SNCoreEditor.UI.HotBar;

namespace SNCoreEditor.UI.Workspace;


public static class WorkspaceRegistration
{
    private static readonly Dictionary<Type, WorkspaceDefinition> _workspaceTypeMap = new();

    private static readonly List<WorkspaceDefinition> _exclusiveWorkspaces = new();
    private static WorkspaceDefinition _coreWorkspace;
    
    public static void Register<T>(WorkspaceDefinition definition) where T : IWorkspace, new()
    {
        if (_workspaceTypeMap.ContainsKey(typeof(T)))
            throw new Exception($"Workspace already registered: {nameof(T)}");

        if (typeof(ICoreWorkspace).IsAssignableFrom(typeof(T)))
        {
            if (_coreWorkspace != null) 
            {
                throw new Exception($"Cannot Core workspace: {nameof(T)}, " +
                                    $"a core workspace already is registered!");
            }
            _coreWorkspace = definition;
            return;
        }

        _workspaceTypeMap.Add(typeof(T), definition);
        _exclusiveWorkspaces.Add(definition);
    }

    internal static WorkspaceDefinition GetCoreWorkspace() => _coreWorkspace;
    
    public static IEnumerable<WorkspaceDefinition> GetExclusiveWorkspaces() 
        => _exclusiveWorkspaces;
}