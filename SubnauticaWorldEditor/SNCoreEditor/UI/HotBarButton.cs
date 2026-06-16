using UnityEngine;
using UnityEngine.UI;

namespace SNCoreEditor.UI;

[RequireComponent(typeof(Button))]
internal class HotBarButton : MonoBehaviour
{
    private Button Button;
    public void Awake()
    {
        Button = gameObject.EnsureComponent<Button>();
    }
}