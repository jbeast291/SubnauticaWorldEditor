using UnityEngine;
using UnityEngine.EventSystems;

namespace SNCoreEditor.UI.Windowing
{
    [RequireComponent(typeof(RectTransform))]
    internal class WindowDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        [SerializeField] private RectTransform rt;
        [SerializeField] private EditorCanvasManager cManager;

        private Window window;
        private Vector2 initialOffset;

        public void Start()
        {
            rt = GetComponent<RectTransform>();
            window = GetComponentInParent<Window>();
            cManager = GetComponentInParent<EditorCanvasManager>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            window.PushToTop();

            initialOffset = cManager.TransformToCanvasSpace(eventData.position) - window.Position();
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 targetPos = cManager.TransformToCanvasSpace(eventData.position) - initialOffset;
            
            targetPos.y += rt.localPosition.y;
            Rect cRect = cManager.GetCanvasRect();
            targetPos.x = Mathf.Clamp(targetPos.x, cRect.xMin, cRect.xMax);
            targetPos.y = Mathf.Clamp(targetPos.y, cRect.yMin, cRect.yMax);

            targetPos.y -= rt.localPosition.y;

            window.SetPosTo(targetPos);
        }
    }
}