using SNCoreEditor.Input;
using UnityEngine;

namespace SNCoreEditor.UI;

public class EditorCanvasManager : uGUI_InputGroup
{
    internal static EditorCanvasManager main { get; private set; }
    
    internal void OnInstantiate()
    {
        if (main != null)
        {
            Plugin.Logger.LogError("Duplicate CoreEditorCanvasManager detected!");
            DestroyImmediate(this);
            return;
        }
        main = this;
        gameObject.SetActive(false);
    }

    private new void Update()
    {
        if (GameInput.GetButtonDown(InputRegistration.ControlCamera))
        {
            if(selected) OnDeselect();// allow the camera to be moved by mouse
            else Select();// show mouse
        }
    }
    
    internal bool IsVisible() => gameObject.activeSelf; 
    
    internal void ToggleEditorVisibility() => gameObject.SetActive(!IsVisible());

    private void OnEnable() => Select();
}