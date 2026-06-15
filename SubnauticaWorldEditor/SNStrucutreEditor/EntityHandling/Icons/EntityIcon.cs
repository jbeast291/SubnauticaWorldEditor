using UnityEngine;

namespace SNStructureEditor.EntityHandling.Icons;

public abstract class EntityIcon
{
    public abstract Sprite Sprite { get; }
    public abstract Color ColorMultiplier { get; }
}