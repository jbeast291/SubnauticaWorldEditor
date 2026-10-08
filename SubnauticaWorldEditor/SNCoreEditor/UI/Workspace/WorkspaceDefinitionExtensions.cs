using System.Runtime.Serialization;
using SNCoreEditor.UI.HotBar.Interfaces;
namespace SNCoreEditor.UI.Workspace;


public static class WorkspaceDefinitionExtensions
{
    extension(WorkspaceDefinition definition)
    {
        public WorkspaceDefinition WithCursorTool<T>() where T : ICursorToolDefinitionProvider
        {
            ICursorToolDefinitionProvider button = (ICursorToolDefinitionProvider) FormatterServices.GetUninitializedObject(typeof(T));
            return definition.WithCursorTool(button!.Definition());
        }
    }
}
