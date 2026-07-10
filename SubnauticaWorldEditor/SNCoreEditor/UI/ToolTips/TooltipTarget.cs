using UnityEngine;
using UnityEngine.EventSystems;

namespace SNCoreEditor.UI.ToolTips;

public class TooltipTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private string defaultTooltipText;
    public bool updateToolTipEachFrame;
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isActiveAndEnabled)
            TooltipManager.Main.AddTarget(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.Main.RemoveTarget(this);
    }

    private void OnDisable()
    {
        TooltipManager.Main.RemoveTarget(this);
    }

    //Yes a field would be better but unity serialization does not like displaying that neatly in 2019 :/
    internal void SetToolTipText(string text) => defaultTooltipText = text;
    internal string GetTooltipText() => defaultTooltipText;
}