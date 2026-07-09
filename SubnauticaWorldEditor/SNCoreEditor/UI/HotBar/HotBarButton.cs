using System.Collections;
using SNCoreEditor.UI.HotBar.Interfaces;
using SNCoreEditor.UI.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace SNCoreEditor.UI.HotBar;

internal class HotBarButton : MonoBehaviour
{
    [SerializeField] private Button Button;
    [SerializeField] private ThemeAssigner themeAssigner;
    [SerializeField] private Image ButtonIcon;
    [SerializeField] private TextMeshProUGUI ButtonHoverText;

    internal HotBarButtonDefinition definition { private get; set; }

    //Action mode
    private IHotBarAction actionListener;
    //Toggle mode
    private IHotBarToggleAction toggleListener;
    private bool toggled = false;
    private Sprite buttonDefaultSprite;
    private Sprite buttonActiveSprite;

    private void Awake()
    {
        themeAssigner.RegisterForOnChange(OnThemeChange);
    }
    
    private void Start()
    {
        Button.onClick.AddListener(OnButtonPressed);
        ButtonIcon.sprite = definition.Icon;
        ButtonHoverText.text = GenerateHotKeyText(definition);
        
        actionListener = definition.HotBarButtonFactory.Invoke();
        if (actionListener is IHotBarToggleAction toggleAction)
        {
            toggleListener = toggleAction;
        }
    }

    private void OnThemeChange()
    {
        buttonDefaultSprite = Button.image.sprite;
        buttonActiveSprite = Button.spriteState.selectedSprite;
        //Theme assigner overwites the base sprite, so set it back if its toggled
        if(toggled) Button.image.sprite = buttonActiveSprite;
    }

    internal void OnButtonPressed()
    {
        if (toggleListener != null)
        {
            HandleToggleOnActivated();
            return;
        }
        HandleActionOnActivated();
    }

    private void SetActive()
    {
        Button.image.sprite = buttonActiveSprite;
        actionListener.OnActivated();//inherited by toggle as well
    }
    
    private void SetDeActive()
    {
        Button.image.sprite = buttonDefaultSprite;
        toggleListener?.OnDeactivated();
    }
    
    private void HandleActionOnActivated()
    {
        SetActive();
        StartCoroutine(SwapSpriteBack());
    }

    private IEnumerator SwapSpriteBack()
    {
        yield return new WaitForSecondsRealtime(0.1f);
        SetDeActive();
    }
    
    private void HandleToggleOnActivated()
    {
        toggled = !toggled;
        if (toggled)
        {
            SetActive();
            return;
        }
        SetDeActive();
    }

    private static string GenerateHotKeyText(HotBarButtonDefinition definition)
    {
        string text = "";
        for (int i = 0; i < definition.buttons.Count; i++)
        {
            text += GameInput.FormatButton(definition.buttons[i]);
            
            if (i < definition.buttons.Count - 1) text += " + ";
        }
        return text;
    }
}