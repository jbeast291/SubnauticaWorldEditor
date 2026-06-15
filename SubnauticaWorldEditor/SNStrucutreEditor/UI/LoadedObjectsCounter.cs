using SNStructureEditor.StructureHandling;
using TMPro;
using UnityEngine;

namespace SNStructureEditor.UI;

public class LoadedObjectsCounter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;
    
    private void Update()
    {
        if (StructureInstance.Main == null)
        {
            text.text = "N/A";
            return;
        }

        text.text = $"{StructureInstance.Main.GetLoadedEntityCount()}/{StructureInstance.Main.GetTotalEntityCount()}";
    }
}