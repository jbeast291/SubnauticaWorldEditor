using UnityEngine;

namespace SNStructureEditor.UI;

public class StructureHelperMenuBase : MonoBehaviour
{
    public StructureHelperUI ui;

    private void OnValidate()
    {
        if (ui == null) ui = GetComponentInParent<StructureHelperUI>();
    }
}