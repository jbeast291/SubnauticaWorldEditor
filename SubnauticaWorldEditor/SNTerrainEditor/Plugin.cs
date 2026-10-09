using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Nautilus.Handlers;
using Sentry;
using SNCoreEditor.UI.Workspace;
using SNTerrainEditor.Core;
using SNTerrainEditor.Workspace;
using SNTerrainEditor.Workspace.Button;
using Unity.Burst;
using Unity.Burst.LowLevel;

namespace SNTerrainEditor;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInDependency("com.snmodding.nautilus")]
public class Plugin : BaseUnityPlugin
{
    private static ManualLogSource? LOGGER = null;

    internal static Assembly Assembly { get; } = Assembly.GetExecutingAssembly();

    private bool WorkspaceRegistered;

    private static bool ExtractCompilerFlags(Type jobType, out string flags)
    {
        flags = string.Empty;
        return false;
    }

    private void Awake()
    {
        LOGGER = Logger;

        //https://download.packages.unity.com/com.unity.burst/-/com.unity.burst-1.6.6.tgz

        Harmony.CreateAndPatchAll(Assembly, $"{PluginInfo.PLUGIN_GUID}");

        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

        WaitScreenHandler.RegisterEarlyAsyncLoadTask(PluginInfo.PLUGIN_NAME, Assets.LoadAssetBundle,
            "Loading Bundle");

        WaitScreenHandler.RegisterEarlyLoadTask(PluginInfo.PLUGIN_NAME, RegisterTerrainWorkspace,
            "Registering Terrain Workspace");

        WaitScreenHandler.RegisterLoadTask(PluginInfo.PLUGIN_NAME, InitTerrainEditor,
            "Initialize Editor");
    }



    internal static void LogDebug(string message) => LOGGER?.LogDebug(message);
    internal static void LogInfo(string message) => LOGGER?.LogInfo(message);
    internal static void LogWarning(string message) => LOGGER?.LogWarning(message);
    internal static void LogError(string message) => LOGGER?.LogError(message);
    internal static void LogFatal(string message) => LOGGER?.LogFatal(message);

    private void InitTerrainEditor(WaitScreenHandler.WaitScreenTask task)
    {
        LargeWorldStreamer.main.gameObject.EnsureComponent<TEBatchManager>();
    }

    public void RegisterTerrainWorkspace(WaitScreenHandler.WaitScreenTask task)
    {
        if (WorkspaceRegistered) return;

        WorkspaceDefinition definition =
            new WorkspaceDefinition("Terrain", null, () => new TerrainWorkspace())
                .WithCursorTool<AddTool>()
                .WithCursorTool<RemoveTool>();

        WorkspaceRegistration.Register<TerrainWorkspace>(definition);
        WorkspaceRegistered = true;
    }

    public static string GetModDirectory()
        => Path.GetDirectoryName(Assembly.Location)!;
}