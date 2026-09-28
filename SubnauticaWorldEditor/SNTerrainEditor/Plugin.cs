using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Nautilus.Handlers;
using SNCoreEditor.UI.Workspace;
using SNTerrainEditor.Core;
using SNTerrainEditor.Workspace;
using SNTerrainEditor.Workspace.Button;

namespace SNTerrainEditor;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInDependency("com.snmodding.nautilus")]
public class Plugin : BaseUnityPlugin { 
    private static ManualLogSource? LOGGER = null;

    internal static Assembly Assembly { get; } = Assembly.GetExecutingAssembly();

    private bool WorkspaceRegistered;
    
    private void Awake() {
        LOGGER = Logger;
        
        Harmony.CreateAndPatchAll(Assembly, $"{PluginInfo.PLUGIN_GUID}");
        
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        WaitScreenHandler.RegisterEarlyAsyncLoadTask(PluginInfo.PLUGIN_NAME, Assets.LoadAssetBundle, "Loading Bundle");
        WaitScreenHandler.RegisterEarlyLoadTask(PluginInfo.PLUGIN_NAME, RegisterTerrainWorkspace, "Registering Core Workspace");
        
        WaitScreenHandler.RegisterLoadTask(PluginInfo.PLUGIN_NAME, InitializeEditor, "Initialize Editor");
    }

    internal static void LogDebug(string message) => LOGGER?.LogDebug(message);
    internal static void LogInfo(string message) => LOGGER?.LogInfo(message);
    internal static void LogWarning(string message) => LOGGER?.LogWarning(message);
    internal static void LogError(string message) => LOGGER?.LogError(message);
    internal static void LogFatal(string message) => LOGGER?.LogFatal(message);
    
    private void InitializeEditor(WaitScreenHandler.WaitScreenTask task)
    {
        LargeWorldStreamer.main.gameObject.EnsureComponent<EditorBatchManager>();
    }
    
    public void RegisterTerrainWorkspace(WaitScreenHandler.WaitScreenTask task)
    {
        if (WorkspaceRegistered) return;

        WorkspaceDefinition definition = new WorkspaceDefinition("Terrain", null, WorkspaceMode.Exclusive, () => new TerrainWorkspace())
            .WithHotBarButton<CheckMarkButton>();

        WorkspaceRegistration.Register<TerrainWorkspace>(definition);
        WorkspaceRegistered = true;
    }
}