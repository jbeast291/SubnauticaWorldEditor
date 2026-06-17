using System;
using System.Reflection;
using Nautilus.Handlers;

namespace SNCoreEditor.Input;

public static class InputRegistration
{
    private const string GeneralCategory = "WorldEditorGeneral";
    private const string ToolsCategory = "WorldEditorTools";
    private const string ModifiersCategory = "WorldEditorModifiers";
    private const string OtherCategory = "WorldEditorOther";

    internal static void RegisterLocalization()
    {
        FieldInfo[] fields = typeof(InputRegistration).GetFields();
        foreach (FieldInfo field in fields)
        {
            EnglishTranslationAttribute translationAttribute = field.GetCustomAttribute<EnglishTranslationAttribute>();
            if (translationAttribute == null)
            {
                Plugin.Logger.LogWarning("Missing English translation for binding " + field.Name);
                continue;
            }

            GameInput.Button buttonValue = (GameInput.Button)field.GetValue(null);
            LanguageHandler.SetLanguageLine("Option" + buttonValue, translationAttribute.Translation);
        }
        
        LanguageHandler.SetLanguageLine(GeneralCategory, "<u>World Editor</u>: General");
        LanguageHandler.SetLanguageLine(ToolsCategory, "<u>World Editor</u>: Tools");
        LanguageHandler.SetLanguageLine(ModifiersCategory, "<u>World Editor</u>: Modifiers");
        LanguageHandler.SetLanguageLine(OtherCategory, "<u>World Editor</u>: Other");
    }
    
    // GENERAL
    [EnglishTranslation("Toggle world editor UI")]
    public static readonly GameInput.Button ToggleEditorKeyBind = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_ToggleEditorKeyBind")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.F4)
        .AvoidConflicts()
        .WithCategory(GeneralCategory);

    [EnglishTranslation("Save (w/ modifier)")]
    public static readonly GameInput.Button SaveKeyBind = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_SaveKeyBind")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.S)
        .AvoidConflicts()
        .WithCategory(GeneralCategory);
    
    [EnglishTranslation("Interact")]
    public static readonly GameInput.Button Interact = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_Interact")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Mouse.LeftButton)
        .AvoidConflicts()
        .WithCategory(GeneralCategory);
    
    [EnglishTranslation("Control camera (with editor open)")]
    public static readonly GameInput.Button ControlCamera = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_ControlCamera")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Mouse.RightButton)
        .AvoidConflicts()
        .WithCategory(GeneralCategory);

    // TOOLS
    [EnglishTranslation("Activate select mode")]
    public static readonly GameInput.Button SelectBind = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_SelectBind")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.Q)
        .AvoidConflicts()
        .WithCategory(ToolsCategory);

    [EnglishTranslation("Translate")]
    public static readonly GameInput.Button TranslateBind = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_TranslateBind")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.E)
        .AvoidConflicts()
        .WithCategory(ToolsCategory);

    [EnglishTranslation("Rotate")]
    public static readonly GameInput.Button RotateBind = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_RotateBind")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.R)
        .AvoidConflicts()
        .WithCategory(ToolsCategory);

    [EnglishTranslation("Scale")]
    public static readonly GameInput.Button ScaleBind = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_ScaleBind")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.T)
        .AvoidConflicts()
        .WithCategory(ToolsCategory);

    [EnglishTranslation("Activate drag & drop tool")]
    public static readonly GameInput.Button DragBind = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_DragBind")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.F)
        .AvoidConflicts()
        .WithCategory(ToolsCategory);

    [EnglishTranslation("Open entity browser")]
    public static readonly GameInput.Button EntityEditorBind = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_EntityEditorBind")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.Tab)
        .AvoidConflicts()
        .WithCategory(ToolsCategory);

    [EnglishTranslation("Activate brush tool")]
    public static readonly GameInput.Button PaintBrushBind = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_PaintBrushBind")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.B)
        .AvoidConflicts()
        .WithCategory(ToolsCategory);

    [EnglishTranslation("Toggle global space")]
    public static readonly GameInput.Button ToggleGlobalSpaceBind = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_ToggleGlobalSpaceBind")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.G)
        .AvoidConflicts()
        .WithCategory(ToolsCategory);

    [EnglishTranslation("Toggle snapping")]
    public static readonly GameInput.Button ToggleSnappingBind = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_ToggleSnappingBind")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.P)
        .AvoidConflicts()
        .WithCategory(ToolsCategory);
    
    [EnglishTranslation("Enable snapping temporarily")]
    public static readonly GameInput.Button HoldToSnap = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_HoldToSnap")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.LeftCtrl)
        .AvoidConflicts()
        .WithCategory(ModifiersCategory);

    [EnglishTranslation("Pick object for brushing")]
    public static readonly GameInput.Button PickObjectBind = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_PickObjectBind")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.K)
        .AvoidConflicts()
        .WithCategory(ToolsCategory);
    
    [EnglishTranslation("Quick pick entity")]
    public static readonly GameInput.Button QuickPickEntity = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_QuickPickEntityBind")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Mouse.MiddleButton)
        .AvoidConflicts()
        .WithCategory(ToolsCategory);

    [EnglishTranslation("Toggle cable editor")]
    public static readonly GameInput.Button CableEditorBind = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_CableEditorBind")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.M)
        .AvoidConflicts()
        .WithCategory(ToolsCategory);

    [EnglishTranslation("Duplicate (w/ modifier)")]
    public static readonly GameInput.Button DuplicateBind = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_DuplicateBind")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.D)
        .AvoidConflicts()
        .WithCategory(ToolsCategory);

    [EnglishTranslation("Select all (w/ modifier)")]
    public static readonly GameInput.Button SelectAllBind = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_SelectAllBind")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.H)
        .AvoidConflicts()
        .WithCategory(ToolsCategory);

    [EnglishTranslation("Undo (w/ modifier)")]
    public static readonly GameInput.Button UndoBind = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_UndoBind")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.Z)
        .AvoidConflicts()
        .WithCategory(ToolsCategory);

    [EnglishTranslation("Select last selected (w/ alt modifier)")]
    public static readonly GameInput.Button SelectLastSelectedBind = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_SelectLastSelectedBind")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.Z)
        .AvoidConflicts()
        .WithCategory(ToolsCategory);
    
    [EnglishTranslation("Delete")]
    public static readonly GameInput.Button DeleteBind = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_DeleteBind")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.Delete)
        .AvoidConflicts()
        .WithCategory(ToolsCategory);
    
    // MODIFIERS
    
    [EnglishTranslation("Prioritize triggers when selecting")]
    public static readonly GameInput.Button PrioritizeTriggers = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_PrioritizeTriggers")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.Alt)
        .AvoidConflicts()
        .WithCategory(ModifiersCategory);

    [EnglishTranslation("Scale uniformly modifier")]
    public static readonly GameInput.Button ScaleUniform = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_ScaleUniform")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.Alt)
        .AvoidConflicts()
        .WithCategory(ModifiersCategory);
    
    [EnglishTranslation("Save hotkey modifier")]
    public static readonly GameInput.Button SaveHotkeyModifier = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_SaveHotkeyModifier")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.LeftCtrl)
        .AvoidConflicts()
        .WithCategory(ModifiersCategory);
    
    [EnglishTranslation("Control tool modifier")]
    public static readonly GameInput.Button ToolHotkeyModifier = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_ToolHotkeyModifier")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.LeftCtrl)
        .AvoidConflicts()
        .WithCategory(ModifiersCategory);
    
    [EnglishTranslation("Alt tool modifier")]
    public static readonly GameInput.Button AltToolHotkeyModifier = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_AltToolHotkeyModifier")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.LeftAlt)
        .AvoidConflicts()
        .WithCategory(ModifiersCategory);
    
    [EnglishTranslation("Select: select multiple")]
    public static readonly GameInput.Button SelectMultipleModifier = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_SelectMultipleModifier")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.LeftCtrl)
        .AvoidConflicts()
        .WithCategory(ModifiersCategory);
    
    // OTHER
    
    [EnglishTranslation("Brush: rotate left")]
    public static readonly GameInput.Button BrushRotateLeft = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_BrushRotateLeft")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.LeftBracket, GameInputHandler.Paths.Mouse.ScrollUp)
        .AvoidConflicts()
        .WithCategory(OtherCategory);

    [EnglishTranslation("Brush: rotate right")]
    public static readonly GameInput.Button BrushRotateRight = EnumHandler
        .AddEntry<GameInput.Button>("SNWECBrushRotateRight")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.RightBracket, GameInputHandler.Paths.Mouse.ScrollDown)
        .AvoidConflicts()
        .WithCategory(OtherCategory);
    
    [EnglishTranslation("Brush: decrease scale")]
    public static readonly GameInput.Button BrushDecreaseScale = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_BrushDecreaseScale")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.Minus)
        .AvoidConflicts()
        .WithCategory(OtherCategory);

    [EnglishTranslation("Brush: increase scale")]
    public static readonly GameInput.Button BrushIncreaseScale = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_BrushIncreaseScale")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.Equals)
        .AvoidConflicts()
        .WithCategory(OtherCategory);

    [EnglishTranslation("Brush/drag: use global up normal")]
    public static readonly GameInput.Button UseGlobalUpNormal = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_UseGlobalUpNormal")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.Semicolon)
        .AvoidConflicts()
        .WithCategory(OtherCategory);
    
    [EnglishTranslation("Browser: go back")]
    public static readonly GameInput.Button GoBack = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_GoBack")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Mouse.BackButton)
        .AvoidConflicts()
        .WithCategory(OtherCategory);
    
    [EnglishTranslation("Browser: go forward")]
    public static readonly GameInput.Button GoForward = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_GoForward")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Mouse.ForwardButton)
        .AvoidConflicts()
        .WithCategory(OtherCategory);

    private class EnglishTranslationAttribute : Attribute
    {
        public string Translation { get; }

        public EnglishTranslationAttribute(string translation)
        {
            Translation = translation;
        }
    }
}