using System;
using System.Collections.Generic;
using SNCoreEditor.UI.HotBar;
using UnityEngine;
namespace SNCoreEditor.UI.Workspace;

/// <summary>
/// Represents non-state based information about the workspace
/// ie, data that's persistent between saves for this workspace
/// </summary>
public sealed record WorkspaceDefinition(
    string NameKey,
    Sprite Icon,
    WorkspaceMode Mode,
    Func<IWorkspace> WorkspaceFactory)
{
    internal List<HotBarButtonDefinition> HotbarButtons { get; } = new();
    
    public WorkspaceDefinition WithHotBarButton(HotBarButtonDefinition button)
    {
        HotbarButtons.Add(button);
        return this;
    }
}

public enum WorkspaceMode
{
    Persistent,
    Exclusive
}