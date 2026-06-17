using System;
using System.Collections;
using System.IO;
using Nautilus.Handlers;
using UnityEngine;

namespace SNCoreEditor;

internal static class Assets
{
    private const string BundleName = "coreassets";
    
    internal static AssetBundle CoreBundle { get; private set; }
    
    internal static IEnumerator LoadCoreAssetBundle(WaitScreenHandler.WaitScreenTask task)
    {
        if (Plugin.Initialized || CoreBundle != null) yield break;
        
        var assetBundleRequest = AssetBundle.LoadFromFileAsync(Path.Combine(Path.GetDirectoryName(Plugin.Assembly.Location)!, "Assets", BundleName));
        yield return assetBundleRequest;
        if(assetBundleRequest.assetBundle == null) throw new Exception("Failed to load core asset bundle!");
        CoreBundle = assetBundleRequest.assetBundle;
        
        Plugin.Initialized = true;
    }
    
    
}