using UnityEngine;
using UnityEngine.UI;

namespace SNCoreEditor.UI;

public class LeftSideScrollRect : ScrollRect
{
    public override void SetLayoutHorizontal()
    {
        base.SetLayoutHorizontal();
        if (verticalScrollbar.IsActive())
        {
            SetContentIndent(-verticalScrollbarSpacing);
        }
        else
        {
            SetContentIndent(0);
        }
    }
    
    public void SetContentIndent(float val) => 
        content.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, val, content.rect.width);
}