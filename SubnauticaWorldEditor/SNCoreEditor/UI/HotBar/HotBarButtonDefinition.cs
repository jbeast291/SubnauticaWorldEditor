using System;
using System.Collections.Generic;
using SNCoreEditor.UI.HotBar.Interfaces;
using UnityEngine;
namespace SNCoreEditor.UI.HotBar;


public sealed record HotBarButtonDefinition(
    Sprite Icon, 
    string hoverLanguageKey,
    List<GameInput.Button> buttons,
    Func<IHotBarAction> HotBarButtonFactory);