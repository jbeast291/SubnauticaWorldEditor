using System;
using UnityEngine;
namespace SNCoreEditor.UI.HotBar;


public sealed record HotBarButtonDefinition(Sprite Icon, Func<IHotBarButton> HotBarButtonFactory);
