using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class FMODAssetCreator
{
    [MenuItem("Assets/Create/Subnautica/Create Vanilla FMOD Asset")]
    public static void CreateFMODAsset()
    {

        FMODAsset fmodAsset = ScriptableObject.CreateInstance<FMODAsset>();
        fmodAsset.name = "NewFMODAsset";
        fmodAsset.path = "event:/";

        string savePath = AssetDatabase.GenerateUniqueAssetPath(
            Path.Combine(GetSelectedFolderPath(), "NewFMODAsset.asset")
        );

        AssetDatabase.CreateAsset(fmodAsset, savePath);
        AssetDatabase.SaveAssets();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = fmodAsset;
    }

    private static string GetSelectedFolderPath()
    {
        Object selected = Selection.activeObject;
        if (!selected) return "Assets";

        string path = AssetDatabase.GetAssetPath(selected);
        if (string.IsNullOrEmpty(path)) return "Assets";

        return Directory.Exists(path) ? path : Path.GetDirectoryName(path);
    }
}
