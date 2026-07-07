using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Nautilus.Handlers;
using Nautilus.Utility.AttributeRegistration;
using SNCoreEditor.CoreWorkspaceStuff;
using SNCoreEditor.CoreWorkspaceStuff.Buttons;
using SNCoreEditor.Input;
using SNCoreEditor.UI;
using SNCoreEditor.UI.HotBar;
using SNCoreEditor.UI.Theme;
using SNCoreEditor.UI.Workspace;
using UnityEngine;

namespace SNCoreEditor;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInDependency("com.snmodding.nautilus")]
internal class Plugin : BaseUnityPlugin
{
    internal static Plugin Instance { get; private set; }
    internal new static ManualLogSource Logger { get; private set; }
    internal static Assembly Assembly { get; } = Assembly.GetExecutingAssembly();
    
    internal static bool Initialized;

    private void Awake()
    {
        Logger = base.Logger;
        Instance = this;
        
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        InputRegistration.RegisterLocalization();
        

        Harmony.CreateAndPatchAll(Assembly, $"{PluginInfo.PLUGIN_GUID}");
        // One time initialization
        WaitScreenHandler.RegisterEarlyAsyncLoadTask(PluginInfo.PLUGIN_NAME, Assets.LoadCoreAssetBundle, "Loading Bundle");

        // Load on every game start
        WaitScreenHandler.RegisterEarlyLoadTask(PluginInfo.PLUGIN_NAME, ThemeManager.Initialize, "Initializing Core Themes");
        WaitScreenHandler.RegisterEarlyLoadTask(PluginInfo.PLUGIN_NAME, RegisterCoreWorkspace, "Registering Core Workspace");
        WaitScreenHandler.RegisterAsyncLoadTask(PluginInfo.PLUGIN_NAME, CanvasInitializer.InstantiateCanvas, "Loading Canvas");
        WaitScreenHandler.RegisterLateLoadTask(PluginInfo.PLUGIN_NAME, InputHandler.CreateInputHandler, "Create Input Handler");
    }

    public void RegisterCoreWorkspace(WaitScreenHandler.WaitScreenTask task)
    {
        WorkspaceDefinition definition = new WorkspaceDefinition("Core", null, WorkspaceMode.Persistent, () => new CoreWorkspace())
            .WithHotBarButton(new HotBarButtonDefinition(Assets.CoreBundle.LoadAsset<Sprite>("UndoIcon"), () => new UndoButton()));
        
        WorkspaceRegistration.Register<CoreWorkspace>(definition);
    }
}