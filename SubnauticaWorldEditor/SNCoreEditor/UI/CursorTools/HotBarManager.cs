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
    
    private readonly Dictionary<CursorToolDefinition, HotBarButton> buttonMap = new();

    private void Start()
    {
        ConstructWorkspaces();
    }

    private void Update()
    {
        foreach (KeyValuePair<CursorToolDefinition, HotBarButton> button in buttonMap)
        {
            if(GameInput.GetHotKeyComboDown(button.Key.buttons)) button.Value.OnButtonPressed();
        }
    }
    
    internal void DeactivateOthers(HotBarButton activeButton) {
        foreach (var otherButton in buttonMap.Values) {
            if(otherButton != activeButton) otherButton.SetDeActive(false);
        }
    }


    private void ConstructWorkspaces()
    {
        List<WorkspaceDefinition> workspaces = WorkspaceRegistration.GetAllWorkspaces();
        foreach (WorkspaceDefinition workspace in workspaces)
        {
            foreach (CursorToolDefinition button in workspace.CursorTools)
            {
                CreateHotBarButton(button);
            }
        }
    }

    private void CreateHotBarButton(CursorToolDefinition button)
    {
        GameObject buttonObj = Instantiate(HotBarButtonPrefab, HotBarContent);
        HotBarButton hotbarButton = buttonObj.GetComponent<HotBarButton>();
        hotbarButton.Init(button, this);
        HotBarGlyphText hotbarText = buttonObj.GetComponentInChildren<HotBarGlyphText>();
        hotbarText.definition = button;
        buttonMap.Add(button, hotbarButton);
    }
}
