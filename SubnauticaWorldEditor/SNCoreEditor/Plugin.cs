using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Nautilus.Handlers;
using Nautilus.Utility.AttributeRegistration;
using SNCoreEditor.UI;

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
        // set project-scoped logger instance
        Logger = base.Logger;
        Instance = this;

        // register harmony patches, if there are any
        
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        
        // One time initialization
        WaitScreenHandler.RegisterEarlyLoadTask(PluginInfo.PLUGIN_NAME, PatchHarmonyMethods, "Patching");
        WaitScreenHandler.RegisterEarlyAsyncLoadTask(PluginInfo.PLUGIN_NAME, Assets.LoadCoreAssetBundle, "Loading Bundle");
        
        // Load Canvas
        WaitScreenHandler.RegisterAsyncLoadTask(PluginInfo.PLUGIN_NAME, CanvasInitializer.InstantiateCanvas, "Loading Canvas");
    }
    
    private static void PatchHarmonyMethods(WaitScreenHandler.WaitScreenTask task)
    {
        if (Initialized) return;
        Harmony.CreateAndPatchAll(Assembly, $"{PluginInfo.PLUGIN_GUID}");
    }
}