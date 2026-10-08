using System;
using SNCoreEditor.UI.HotBar.Interfaces;
using SNCoreEditor.UI.Theme;
using SNCoreEditor.UI.ToolTips;
using UnityEngine;
using UnityEngine.UI;
namespace SNCoreEditor.UI.HotBar;

//TODO: move to unity toggle and not bs sprite swapping,
//  will need to edit themes for this as well
internal class HotBarButton : MonoBehaviour {
    [SerializeField] private Button Button;
    [SerializeField] private ThemeAssigner themeAssigner;
    [SerializeField] private Image ButtonIcon;
    [SerializeField] private TooltipTarget tooltipTarget;

    private CursorToolDefinition _definition;
    private HotBarManager _manager;

    private ICursorTool actionListener;
    private bool toggled = false;
    private Sprite buttonDefaultSprite;
    private Sprite buttonActiveSprite;

    internal void Init(CursorToolDefinition definition, HotBarManager manager) {
        if (_definition != null) {
            throw new Exception($"Cannot initialize cursor tool {definition.ID}!" +
                                $"button already initialized to {_definition.ID}");
        }
        _definition = definition;
        _manager = manager;
    }
    
    private void Awake()
    {
        themeAssigner.RegisterForOnChange(OnThemeChange);
    }
    
    private void Start()
    {
        Button.onClick.AddListener(() => OnButtonPressed());
        ButtonIcon.sprite = _definition.Icon;
        
        actionListener = _definition.HotBarButtonFactory.Invoke();
        tooltipTarget.SetToolTipText(Language.main.Get(_definition.ID));
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
        ToggleOnActivated(sendEvents);
    }
    
    internal void SetActive(bool sendEvents)
    {
        Button.image.sprite = buttonActiveSprite;
        toggled = true;
        _manager.DeactivateOthers(this);
        if(sendEvents) actionListener.OnActivated();
    }
    
    internal void SetDeActive(bool sendEvents)
    {
        Button.image.sprite = buttonDefaultSprite;
        toggled = false;
        
        if(sendEvents) actionListener?.OnDeactivated();
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
}