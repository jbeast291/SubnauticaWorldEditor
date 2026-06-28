using UnityEngine;
using UnityEngine.UI;

namespace SNCoreEditor.UI;

internal abstract class HotBarButton : MonoBehaviour
{
    private Button Button;
    public void Awake()
    {
        Button = gameObject.EnsureComponent<Button>();
    }
}