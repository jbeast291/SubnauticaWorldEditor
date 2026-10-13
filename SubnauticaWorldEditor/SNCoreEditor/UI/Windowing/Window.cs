using UnityEngine;
namespace SNCoreEditor.UI.Windowing;


internal class Window : MonoBehaviour
{
    [SerializeField] private RectTransform rt;

    private void OnValidate()
    {
        rt = GetComponent<RectTransform>();
    }

    internal void SetPosTo(Vector2 newPosition) => rt.localPosition = newPosition;

    internal Vector2 Position() => rt.localPosition;

    internal void PushToTop() { 
        transform.SetAsLastSibling();
    }
}
