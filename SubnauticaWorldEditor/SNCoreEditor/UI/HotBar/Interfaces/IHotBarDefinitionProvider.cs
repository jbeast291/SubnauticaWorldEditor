namespace SNCoreEditor.UI.HotBar.Interfaces;


public interface IHotBarDefinitionProvider
{
    [UninitializedContext]
    HotBarButtonDefinition Definition();
}
