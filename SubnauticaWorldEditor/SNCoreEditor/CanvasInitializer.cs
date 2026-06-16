using System;
using System.Collections;
using Nautilus.Handlers;
using UnityEngine;

namespace SNCoreEditor.UI;

internal static class CanvasInitializer
{
    internal static IEnumerator InstantiateCanvas(WaitScreenHandler.WaitScreenTask task)
    {
        Plugin.Logger.LogInfo($"LOADING {Assets.CoreBundle == null}");
        AssetBundleRequest request = Assets.CoreBundle.LoadAssetAsync<GameObject>("SNEditorCanvas");
        yield return request;
        if (request.asset == null) throw new Exception("Failed to load editor canvas!");

        GameObject gameObject = UWE.Utils.InstantiateDeactivated(request.asset as GameObject);
        gameObject.GetComponent<CoreEditorCanvasManager>().OnInstantiate();
        
    }
}