using TMPro;
using UnityEngine;

namespace SNStructureEditor.UI;

public class ApplyFontToChildren : MonoBehaviour
{
    private void Awake()
    {
        foreach (var child in gameObject.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            child.font = Nautilus.Utility.FontUtils.Aller_Rg;
        }
    }
}