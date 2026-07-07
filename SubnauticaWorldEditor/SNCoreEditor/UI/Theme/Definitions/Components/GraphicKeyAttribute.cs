using System;
namespace SNCoreEditor.UI.Theme.Definitions.Components;

[AttributeUsage(AttributeTargets.Class)]
public sealed class GraphicKeyAttribute(string key) : Attribute
{
    public string Key { get; } = key;
}