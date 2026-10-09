using System;
using System.Collections.Generic;
using SNCoreEditor.UI.HotBar;
using UnityEngine;

namespace SNCoreEditor.UI.Workspace;

/// <summary>
/// Represents data that is persistent between saves for a workspace.
/// </summary>
public sealed class WorkspaceDefinition(
    string id,
    Sprite icon,
    Func<IWorkspace> workspaceFactory
) {
    public readonly string ID = id;
    public readonly Sprite Icon = icon;
    public readonly Func<IWorkspace> WorkspaceFactory = workspaceFactory;
    internal readonly List<CursorToolDefinition> CursorTools = new();

    public WorkspaceDefinition WithCursorTool(CursorToolDefinition definition)
    {
        CursorTools.Add(definition);
        return this;
    }
}

public interface IWorkspace
{
    void Initialize();
    
    void OnEnableWorkspace();
    
    void OnDisableWorkspace();

    /*
    void Save(string projectPath);
    
    void Export(string modFolder);
    */
}

public interface ICoreWorkspace : IWorkspace { }