using System.Collections.Generic;
using SNCoreEditor.UI.Workspace;
using UnityEngine;
namespace SNCoreEditor.UI.HotBar;

public class HotBarManager : MonoBehaviour
{
    [SerializeField] private GameObject HotBarButtonPrefab;
    [SerializeField] private Transform HotBarContent;

    public void Start()
    {
        Plugin.Logger.LogError("HOTBAR START CALLED()");
        ConstructUI();
    } 

    public void ConstructUI()
    {
        List<WorkspaceDefinition> workspaces = WorkspaceRegistration.GetAllWorkspaces();
        foreach (WorkspaceDefinition workspace in workspaces)
        {
            foreach (HotBarButtonDefinition button in workspace.HotbarButtons)
            {
                CreateHotBarButton(button);
            }
        }
    }

    public void CreateHotBarButton(HotBarButtonDefinition button)
    {
        Instantiate(HotBarButtonPrefab, HotBarContent);
    }
}
