using SNStructureEditor.UI;
using UnityEngine;

namespace SNStructureEditor.Interfaces;

public interface IOverlayIconData
{
    public string Label { get; }
    public Sprite Icon { get; }
    public Vector3 Position { get; }
    public float Scale { get; }
    public void OnCreation(OverlayIconInstance instance);
}