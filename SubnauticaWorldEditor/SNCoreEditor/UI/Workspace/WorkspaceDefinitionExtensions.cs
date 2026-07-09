using System.Runtime.Serialization;
using SNCoreEditor.UI.HotBar.Interfaces;
namespace SNCoreEditor.UI.Workspace;


public static class WorkspaceDefinitionExtensions
{
    extension(WorkspaceDefinition definition)
    {
        public WorkspaceDefinition WithHotBarButton<T>() where T : IHotBarDefinitionProvider
        {
            IHotBarDefinitionProvider button = FormatterServices.GetUninitializedObject(typeof(T)) as IHotBarDefinitionProvider;
            return definition.WithHotBarButton(button!.Definition());
        }
    }
}
