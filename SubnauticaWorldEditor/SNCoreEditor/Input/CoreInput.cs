using System;
using System.Reflection;
using Nautilus.Handlers;

namespace SNCoreEditor.Input;

public static class CoreInput
{
    public const string GeneralCategory = "WorldEditorGeneral";
    public const string ToolsCategory = "WorldEditorTools";
    public const string ModifiersCategory = "WorldEditorModifiers";
    public const string OtherCategory = "WorldEditorOther";

    internal static void RegisterLocalization()
    {
        FieldInfo[] fields = typeof(CoreInput).GetFields();
        foreach (FieldInfo field in fields)
        {
            field.GetValue(null);
        }
    }
    
    // GENERAL
    public static readonly GameInput.Button ToggleEditorKeyBind = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_ToggleEditorKeyBind")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.F4)
        .AvoidConflicts()
        .WithCategory(GeneralCategory);
    
    public static readonly GameInput.Button SaveKeyBind = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_SaveKeyBind")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.S)
        .AvoidConflicts()
        .WithCategory(GeneralCategory);
    
    public static readonly GameInput.Button Interact = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_Interact")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Mouse.LeftButton)
        .AvoidConflicts()
        .WithCategory(GeneralCategory);
    
    public static readonly GameInput.Button ControlCamera = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_ControlCamera")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Mouse.RightButton)
        .AvoidConflicts()
        .WithCategory(GeneralCategory);

    // TOOLS
    public static readonly GameInput.Button SelectBind = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_SelectBind")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.Q)
        .AvoidConflicts()
        .WithCategory(ToolsCategory);
    
    public static readonly GameInput.Button TranslateBind = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_TranslateBind")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.E)
        .AvoidConflicts()
        .WithCategory(ToolsCategory);
    
    public static readonly GameInput.Button RotateBind = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_RotateBind")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.R)
        .AvoidConflicts()
        .WithCategory(ToolsCategory);
    
    public static readonly GameInput.Button ScaleBind = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_ScaleBind")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.T)
        .AvoidConflicts()
        .WithCategory(ToolsCategory);
    
    public static readonly GameInput.Button DragBind = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_DragBind")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.F)
        .AvoidConflicts()
        .WithCategory(ToolsCategory);
    
    public static readonly GameInput.Button EntityEditorBind = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_EntityEditorBind")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.Tab)
        .AvoidConflicts()
        .WithCategory(ToolsCategory);
    
    public static readonly GameInput.Button PaintBrushBind = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_PaintBrushBind")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.B)
        .AvoidConflicts()
        .WithCategory(ToolsCategory);
    
    public static readonly GameInput.Button ToggleGlobalSpaceBind = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_ToggleGlobalSpaceBind")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.G)
        .AvoidConflicts()
        .WithCategory(ToolsCategory);
    
    public static readonly GameInput.Button ToggleSnappingBind = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_ToggleSnappingBind")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.P)
        .AvoidConflicts()
        .WithCategory(ToolsCategory);
    
    public static readonly GameInput.Button HoldToSnap = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_HoldToSnap")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.LeftCtrl)
        .AvoidConflicts()
        .WithCategory(ModifiersCategory);
    
    public static readonly GameInput.Button PickObjectBind = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_PickObjectBind")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.K)
        .AvoidConflicts()
        .WithCategory(ToolsCategory);
    
    public static readonly GameInput.Button QuickPickEntity = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_QuickPickEntityBind")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Mouse.MiddleButton)
        .AvoidConflicts()
        .WithCategory(ToolsCategory);
    
    public static readonly GameInput.Button CableEditorBind = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_CableEditorBind")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.M)
        .AvoidConflicts()
        .WithCategory(ToolsCategory);
    
    public static readonly GameInput.Button DuplicateBind = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_DuplicateBind")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.D)
        .AvoidConflicts()
        .WithCategory(ToolsCategory);
    
    public static readonly GameInput.Button SelectAllBind = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_SelectAllBind")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.H)
        .AvoidConflicts()
        .WithCategory(ToolsCategory);
    
    public static readonly GameInput.Button UndoBind = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_UndoBind")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.Z)
        .AvoidConflicts()
        .WithCategory(ToolsCategory);
    
    public static readonly GameInput.Button SelectLastSelectedBind = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_SelectLastSelectedBind")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.Z)
        .AvoidConflicts()
        .WithCategory(ToolsCategory);
    
    public static readonly GameInput.Button DeleteBind = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_DeleteBind")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.Delete)
        .AvoidConflicts()
        .WithCategory(ToolsCategory);
    
    // MODIFIERS
    public static readonly GameInput.Button PrioritizeTriggers = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_PrioritizeTriggers")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.Alt)
        .AvoidConflicts()
        .WithCategory(ModifiersCategory);
    
    public static readonly GameInput.Button ScaleUniform = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_ScaleUniform")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.Alt)
        .AvoidConflicts()
        .WithCategory(ModifiersCategory);
    
    public static readonly GameInput.Button SaveHotkeyModifier = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_SaveHotkeyModifier")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.LeftCtrl)
        .AvoidConflicts()
        .WithCategory(ModifiersCategory);
    
    public static readonly GameInput.Button CtrlModifier = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_ToolHotkeyModifier")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.LeftCtrl)
        .AvoidConflicts()
        .WithCategory(ModifiersCategory);
    
    public static readonly GameInput.Button AltModifier = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_AltToolHotkeyModifier")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.LeftAlt)
        .AvoidConflicts()
        .WithCategory(ModifiersCategory);
    
    public static readonly GameInput.Button SelectMultipleModifier = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_SelectMultipleModifier")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.LeftCtrl)
        .AvoidConflicts()
        .WithCategory(ModifiersCategory);
    
    // OTHER
    public static readonly GameInput.Button BrushRotateLeft = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_BrushRotateLeft")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.LeftBracket, GameInputHandler.Paths.Mouse.ScrollUp)
        .AvoidConflicts()
        .WithCategory(OtherCategory);
    
    public static readonly GameInput.Button BrushRotateRight = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_BrushRotateRight")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.RightBracket, GameInputHandler.Paths.Mouse.ScrollDown)
        .AvoidConflicts()
        .WithCategory(OtherCategory);
    
    public static readonly GameInput.Button BrushDecreaseScale = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_BrushDecreaseScale")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.Minus)
        .AvoidConflicts()
        .WithCategory(OtherCategory);
    
    public static readonly GameInput.Button BrushIncreaseScale = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_BrushIncreaseScale")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.Equals)
        .AvoidConflicts()
        .WithCategory(OtherCategory);
    
    public static readonly GameInput.Button UseGlobalUpNormal = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_UseGlobalUpNormal")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Keyboard.Semicolon)
        .AvoidConflicts()
        .WithCategory(OtherCategory);
    
    public static readonly GameInput.Button GoBack = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_GoBack")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Mouse.BackButton)
        .AvoidConflicts()
        .WithCategory(OtherCategory);
    
    public static readonly GameInput.Button GoForward = EnumHandler
        .AddEntry<GameInput.Button>("SNWEC_GoForward")
        .CreateInput()
        .WithKeyboardBinding(GameInputHandler.Paths.Mouse.ForwardButton)
        .AvoidConflicts()
        .WithCategory(OtherCategory);
}