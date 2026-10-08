using System;
using System.Collections.Generic;
using SNCoreEditor.UI.HotBar;
using UnityEngine;
namespace SNCoreEditor.UI.Workspace;

/// <summary>
/// Represents data that is persistent between saves for a workspace.
/// </summary>
public sealed record WorkspaceDefinition(
    string ID,
    Sprite Icon,
    WorkspaceMode Mode,
    Func<IWorkspace> WorkspaceFactory
) {
    internal List<CursorToolDefinition> CursorTools { get; } = new();

    public WorkspaceDefinition WithCursorTool(CursorToolDefinition definition)
    {
        CursorTools.Add(definition);
        return this;
    }
}

public enum WorkspaceMode {
    Persistent,
    Exclusive
}