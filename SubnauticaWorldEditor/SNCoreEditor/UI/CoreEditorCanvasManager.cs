using System;
using UnityEngine;

namespace SNCoreEditor.UI;

public class CoreEditorCanvasManager : MonoBehaviour
{
    internal static CoreEditorCanvasManager main { get; private set; }
    
    internal void OnInstantiate()
    {
        if (main != null)
        {
            Plugin.Logger.LogError("Duplicate CoreEditorCanvasManager detected!");
            DestroyImmediate(this);
            return;
        }
        main = this;
    }

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    //NOTE: Called the first time the canvas opens
    private void Start()
    {
        
    }

    internal void ToggleEditorVisibility()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }

    internal void HideEditor()
    {
        
    }
}