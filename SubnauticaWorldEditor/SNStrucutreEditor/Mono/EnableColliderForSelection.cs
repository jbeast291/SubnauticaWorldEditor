using System;
using System.Linq;
using SNStructureEditor.Editing.Tools;
using SNStructureEditor.Interfaces;
using SNStructureEditor.StructureHandling;
using SNStructureEditor.UI;
using UnityEngine;

namespace SNStructureEditor.Mono;

public class EnableColliderForSelection : MonoBehaviour, ITransformationListener
{
    public SphereCollider managedCollider;

    private void OnEnable()
    {
        UpdateManagedCollider(GetSelectionColliderShouldEnable());
        var ui = StructureHelperUI.main;
        if (ui != null) ui.toolManager.OnToolStateChangedHandler += OnToolStateChanged;
        StructureInstance.OnStructureInstanceChanged += OnStructureInstanceChanged;
    }

    private void Start()
    {
        UpdateManagedCollider(GetSelectionColliderShouldEnable());
    }

    private void OnDisable()
    {
        UpdateManagedCollider(false);
        var ui = StructureHelperUI.main;
        if (ui != null) ui.toolManager.OnToolStateChangedHandler -= OnToolStateChanged;
        StructureInstance.OnStructureInstanceChanged -= OnStructureInstanceChanged;
    }

    private void OnToolStateChanged(ToolBase tool, bool toolEnabled)
    {
        UpdateManagedCollider(GetSelectionColliderShouldEnable());
    }

    private bool GetSelectionColliderShouldEnable()
    {
        return StructureHelperUI.main.toolManager.tools.Any(tool =>
            tool.ToolEnabled && (tool.Type is ToolType.Select or ToolType.ObjectPicker ||
            tool.Type is ToolType.DragAndDrop && !((DragAndDropTool) tool).Dragging));
    }

    private void OnDestroy()
    {
        Destroy(managedCollider);
    }

    private void UpdateManagedCollider(bool enableCollider)
    {
        if (!managedCollider) return;
        managedCollider.enabled = enableCollider;
        if (enableCollider)
        {
            UpdateColliderScale();
        }
    }

    private void UpdateColliderScale()
    {
        var thisObjectScale = (transform.lossyScale.x + transform.lossyScale.y + transform.lossyScale.z) / 3f;
        // what even are these random numbers I chose?
        managedCollider.radius = Mathf.Clamp(1f / thisObjectScale, 0.00001f, 100000f);
    }

    public void OnStartTransforming()
    {
        
    }

    public void OnFinishTransforming()
    {
        UpdateColliderScale();
    }
    
    private void OnStructureInstanceChanged(StructureInstance newInstance)
    {
        if (newInstance == null)
            Destroy(this);
    }
}