using System.Collections;
using Nautilus.Handlers;
using SNCoreEditor.UI;
using UnityEngine;

namespace SNCoreEditor.Input;

public class InputHandler : MonoBehaviour
{
    internal static void CreateInputHandler(WaitScreenHandler.WaitScreenTask task)
    {
        GameObject inputHandlerObj = new GameObject("WorldEditorInputHandler");
        inputHandlerObj.AddComponent<InputHandler>();
    }

    private IEnumerator Start()
    {
        enabled = false;
        yield return new WaitUntil(() => EditorCanvasManager.main != null);
        enabled = true;
    }
    
    private void Update()
    {
        if (GameInput.GetButtonDown(InputRegistration.ToggleEditorKeyBind))
        {
            EditorCanvasManager.main.ToggleEditorVisibility();
        }
    }
}