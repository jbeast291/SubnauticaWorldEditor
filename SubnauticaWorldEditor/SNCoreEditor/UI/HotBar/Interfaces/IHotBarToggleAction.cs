using System.Collections.Generic;
namespace SNCoreEditor.UI.HotBar.Interfaces;


public interface IHotBarToggleAction : IHotBarAction
{
    List<string> incompatibleWith { get; }
    
    void OnDeactivated();

    void OnUpdate();
}
