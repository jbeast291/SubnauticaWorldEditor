using System;
using System.Collections.Generic;
using SNCoreEditor.UI.HotBar;
using UnityEngine;
namespace SNCoreEditor.UI.Workspace;

/// <summary>
/// Represents data that is persistent between saves for a workspace.
/// </summary>
public sealed record WorkspaceDefinition(
    string NameKey,
    Sprite Icon,
    WorkspaceMode Mode,
    Func<IWorkspace> WorkspaceFactory)
{
    internal List<HotBarButtonDefinition> HotbarButtons { get; } = new();

    public WorkspaceDefinition WithHotBarButton(HotBarButtonDefinition definition)
    {
        HotbarButtons.Add(definition);
        return this;
    }
}

public enum WorkspaceMode
{
    Persistent,
    Exclusive
}