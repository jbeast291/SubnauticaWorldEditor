using System;
using System.Collections.Generic;
using SNCoreEditor.UI.HotBar.Interfaces;
using UnityEngine;
namespace SNCoreEditor.UI.HotBar;


public sealed record CursorToolDefinition(
    string ID,
    Sprite Icon, 
    List<GameInput.Button> buttons,
    Func<ICursorTool> HotBarButtonFactory
);