using System.Collections.Generic;
using SNCoreEditor.Input;
using SNCoreEditor.UI.HotBar.Interfaces;
using SNCoreEditor.UI.Workspace;
using UnityEngine;
namespace SNCoreEditor.UI.HotBar;

public class HotBarManager : MonoBehaviour
{
    [SerializeField] private GameObject HotBarButtonPrefab;
    [SerializeField] private Transform HotBarContent;
    
    private readonly Dictionary<HotBarButtonDefinition, HotBarButton> buttonMap = new();

    private void Start()
    {
        ConstructWorkspaces();
    }

    private void Update()
    {
        foreach (KeyValuePair<HotBarButtonDefinition, HotBarButton> button in buttonMap)
        {
            if(GameInput.GetHotKeyComboDown(button.Key.buttons)) button.Value.OnButtonPressed();
        }
    }

    internal void DeactivateIncompatibleWith(IHotBarToggleAction toggleAction)
    {
        List<string> disableIDs = toggleAction.incompatibleWith;
        foreach (KeyValuePair<HotBarButtonDefinition, HotBarButton> button in buttonMap)
        {
            if(disableIDs.Contains(button.Key.ID)) button.Value.SetDeActive(false);
        }
    }

    private void ConstructWorkspaces()
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

    private void CreateHotBarButton(HotBarButtonDefinition button)
    {
        GameObject buttonObj = Instantiate(HotBarButtonPrefab, HotBarContent);
        HotBarButton hotbarButton = buttonObj.GetComponent<HotBarButton>();
        hotbarButton.definition = button;
        hotbarButton.manager = this;
        buttonMap.Add(button, hotbarButton);
    }
}
