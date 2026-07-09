using System.Collections.Generic;
using SNCoreEditor.Input;
using SNCoreEditor.UI.HotBar.Interfaces;
using SNCoreEditor.UI.Workspace;
using TMPro;
using UnityEngine;
namespace SNCoreEditor.UI.HotBar;

public class HotBarManager : MonoBehaviour
{
    [SerializeField] private GameObject HotBarButtonPrefab;
    [SerializeField] private Transform HotBarContent;

    private List<(List<GameInput.Button> hotKeys, HotBarButton button)> hotkeyMap = new();

    public void Start()
    {
        ConstructWorkspaces();
    }

    public void Update()
    {
        foreach ((List<GameInput.Button> hotKeys, HotBarButton button) in hotkeyMap)
        {
            if(GameInput.GetHotKeyComboDown(hotKeys)) button.OnButtonPressed();
        }
    }

    public void ConstructWorkspaces()
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
        hotkeyMap.Add((button.buttons, hotbarButton));
    }
}
