using SNCoreEditor.Input;
using UnityEngine;
using UnityEngine.UI;

namespace SNCoreEditor.UI;

public class EditorCanvasManager : uGUI_InputGroup
{
    internal static EditorCanvasManager INSTANCE { get; private set; }
    
    [SerializeField] private RectTransform rt;

    private void OnValidate()
    {
        rt = GetComponent<RectTransform>();
    }

    internal void OnInstantiate()
    {
        if (INSTANCE != null)
        {
            Plugin.Logger.LogError("Duplicate CoreEditorCanvasManager detected!");
            DestroyImmediate(this);
            return;
        }
        INSTANCE = this;
        gameObject.SetActive(false);
    }

    private new void Update()
    {
        if (GameInput.GetButtonDown(CoreInput.ControlCamera))
        {
            if(selected) Deselect();// allow the camera to be moved by mouse
            else Select();// show mouse
        }

        if (GameInput.GetButtonHeld(CoreInput.SaveHotkeyModifier) && GameInput.GetButtonDown(CoreInput.SaveKeyBind))
        {
            Plugin.Logger.LogError("SAVE KEYBIND");
            //CoreEditorCanvasManager.TrySave();
        }
    }

    private bool IsEditorVisible() => gameObject.activeSelf; 

    internal void ToggleEditorVisibility() => gameObject.SetActive(!IsEditorVisible());

    private void OnEnable() => Select();

    public Vector2 TransformToCanvasSpace(Vector2 screenPos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenPos, null, out Vector2 localMouse);
        return localMouse;
    }
    
    public Rect GetCanvasRect() => rt.rect;
}