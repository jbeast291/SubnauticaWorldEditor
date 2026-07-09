using System.Collections.Generic;
using SNCoreEditor.UI.HotBar.Interfaces;
using SNCoreEditor.UI.Workspace;
using UnityEngine;
namespace SNCoreEditor.UI.HotBar;

public class HotBarManager : MonoBehaviour
{
    [SerializeField] private GameObject HotBarButtonPrefab;
    [SerializeField] private Transform HotBarContent;

    public void Start()
    {
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
        GameObject buttonObj = Instantiate(HotBarButtonPrefab, HotBarContent);
        HotBarButton hotbarButton = buttonObj.GetComponent<HotBarButton>();
        hotbarButton.definition = button;
    }
}
