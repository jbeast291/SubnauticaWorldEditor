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
    internal HotBarManager manager { private get; set; }

    //Action mode
    private IHotBarAction actionListener;
    //Toggle mode
    private IHotBarToggleAction toggleListener;
    private bool buttonIsToggleAction => toggleListener != null;
    private bool toggled = false;
    private Sprite buttonDefaultSprite;
    private Sprite buttonActiveSprite;

    private void Awake()
    {
        themeAssigner.RegisterForOnChange(OnThemeChange);
    }
    
    private void Start()
    {
        Button.onClick.AddListener(() => OnButtonPressed());
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
        //Theme assigner overwrites the base sprite, so set it back if its toggled
        if(toggled) Button.image.sprite = buttonActiveSprite;
    }

    internal void OnButtonPressed(bool sendEvents = true)
    {
        if (buttonIsToggleAction)
        {
            ToggleOnActivated(sendEvents);
            return;
        }
        ActionOnActivated(sendEvents);
    }
    
    internal void SetActive(bool sendEvents)
    {
        Button.image.sprite = buttonActiveSprite;
        toggled = true;
        
        if(sendEvents) actionListener.OnActivated();//inherited by toggle as well
        if(buttonIsToggleAction) manager.DeactivateIncompatibleWith(toggleListener);
    }
    
    internal void SetDeActive(bool sendEvents)
    {
        Button.image.sprite = buttonDefaultSprite;
        toggled = false;
        
        if(sendEvents) toggleListener?.OnDeactivated();
    }
    
    private void ActionOnActivated(bool sendEvents)
    {
        SetActive(sendEvents);
        StartCoroutine(SwapSpriteBack());
        return;
        
        IEnumerator SwapSpriteBack()
        {
            yield return new WaitForSecondsRealtime(0.1f);
            SetDeActive(sendEvents);
        }
    }
    
    private void ToggleOnActivated(bool sendEvents)
    {
        if (toggled)
        {
            SetDeActive(sendEvents);
            return;
        }
        SetActive(sendEvents);
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