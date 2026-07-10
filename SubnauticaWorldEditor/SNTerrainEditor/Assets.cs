using System;
using System.Collections;
using System.IO;
using Nautilus.Handlers;
using UnityEngine;

namespace SNTerrainEditor;

internal static class Assets
{
    private const string BundleName = "terrainassets";
    
    internal static AssetBundle TerrainBundle { get; private set; }
    
    internal static IEnumerator LoadAssetBundle(WaitScreenHandler.WaitScreenTask task)
    {
        if (TerrainBundle != null) yield break;
        
        AssetBundleCreateRequest assetBundleRequest = AssetBundle.LoadFromFileAsync(Path.Combine(Path.GetDirectoryName(Plugin.Assembly.Location)!, "Assets", BundleName));
        yield return assetBundleRequest;
        if(assetBundleRequest.assetBundle == null) throw new Exception("Failed to load core asset bundle!");
        TerrainBundle = assetBundleRequest.assetBundle;
    }
}
