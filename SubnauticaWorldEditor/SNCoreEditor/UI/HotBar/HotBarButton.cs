using UnityEngine;
using UnityEngine.UI;
namespace SNCoreEditor.UI.HotBar;

internal class HotBarButton : MonoBehaviour
{
    [SerializeField] private Image ButtonIcon;
    [SerializeField] private Button Button;

    internal HotBarButtonDefinition definition { private get; set; }

    private IHotBarButton listener;
    
    public void Start()
    {
        listener = definition.HotBarButtonFactory.Invoke();
        Button.onClick.AddListener(listener.OnActivated);
        ButtonIcon.sprite = definition.Icon;
    }
}