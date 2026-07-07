using UnityEngine;

namespace SNCoreEditor.UI.Workspace;

public interface IWorkspace
{
    //TODO: maybe we have a config object separate that holds this, might clean up contract

    void Initialize();
    
    void OnEnableWorkspace();
    
    void OnDisableWorkspace();

    /*
    void OnPrimaryAction(Vector3 worldPosition);
    
    void Save(string projectPath);
    
    void Export(string modFolder);
    */
}