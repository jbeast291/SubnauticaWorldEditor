using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Nautilus.Handlers;
using SNCoreEditor.CoreWorkspaceStuff;
using SNCoreEditor.CoreWorkspaceStuff.Buttons;
using SNCoreEditor.Input;
using SNCoreEditor.UI.Theme;
using SNCoreEditor.UI.Workspace;

namespace SNCoreEditor;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInDependency("com.snmodding.nautilus")]
internal class Plugin : BaseUnityPlugin
{
    internal static Plugin Instance { get; private set; }
    internal new static ManualLogSource Logger { get; private set; }
    internal static Assembly Assembly { get; } = Assembly.GetExecutingAssembly();

    private static bool CoreRegistered;

    private void Awake()
    {
        Logger = base.Logger;
        Instance = this;
        
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        InputRegistration.RegisterLocalization();
        LanguageHandler.RegisterLocalizationFolder();

        Harmony.CreateAndPatchAll(Assembly, $"{PluginInfo.PLUGIN_GUID}");
        // One time initialization
        WaitScreenHandler.RegisterEarlyAsyncLoadTask(PluginInfo.PLUGIN_NAME, Assets.LoadCoreAssetBundle, "Loading Bundle");
        WaitScreenHandler.RegisterEarlyLoadTask(PluginInfo.PLUGIN_NAME, RegisterCoreWorkspace, "Registering Core Workspace");
        WaitScreenHandler.RegisterEarlyLoadTask(PluginInfo.PLUGIN_NAME, ThemeManager.Initialize, "Initializing Core Themes");

        // Load on every game start
        WaitScreenHandler.RegisterAsyncLoadTask(PluginInfo.PLUGIN_NAME, CanvasInitializer.InstantiateCanvas, "Loading Canvas");
        WaitScreenHandler.RegisterLateLoadTask(PluginInfo.PLUGIN_NAME, InputHandler.CreateInputHandler, "Create Input Handler");
    }

    public void RegisterCoreWorkspace(WaitScreenHandler.WaitScreenTask task)
    {
        if (CoreRegistered) return;
        
        WorkspaceDefinition definition = new WorkspaceDefinition("Core", null, WorkspaceMode.Persistent, () => new CoreWorkspace())
            .WithHotBarButton<UndoButton>()
            .WithHotBarButton<UndoButtonToggle>()
            .WithHotBarButton<UndoButtonToggleTwo>();

        WorkspaceRegistration.Register<CoreWorkspace>(definition);
        CoreRegistered = true;
    }
}