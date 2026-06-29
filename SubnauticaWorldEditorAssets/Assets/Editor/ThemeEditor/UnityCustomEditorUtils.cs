using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SNCoreEditor.UI.Theme.Definitions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class UnityCustomEditorUtils
{
    public static bool ElementRemoveButton(SerializedProperty propertyArray, int index)
    {
        Color prevColor = GUI.color;
        GUI.color = new Color(0.8f, 0.5f, 0.5f);
        if (GUILayout.Button("Remove", GUILayout.Width(70)))
        {
            propertyArray.DeleteArrayElementAtIndex(index);
            return true;
        }
        GUI.color = prevColor;
        return false;
    }

    public static void CreateAssetInRegionFolder(ScriptableObject asset, string baseDirectory, string regionName)
    {
        string regionFolder = Path.Combine(baseDirectory, regionName);
        if (!AssetDatabase.IsValidFolder(regionFolder))
        {
            AssetDatabase.CreateFolder(baseDirectory, regionName);
        }
        string regionFileDefinition = Path.Combine(regionFolder, $"{regionName}.asset");
        CreateAssetAt(asset, regionFileDefinition);
    }

    public static void CreateAssetAt(ScriptableObject asset, string assetPath)
    {
        assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
        AssetDatabase.CreateAsset(asset, assetPath);
        AssetDatabase.SaveAssets();
    }

    public static void AddAssetToArray(SerializedProperty propertyArray, ScriptableObject asset, SerializedObject serializedObject)
    {
        propertyArray.arraySize++;
        SerializedProperty element = propertyArray.GetArrayElementAtIndex(propertyArray.arraySize - 1);
        element.objectReferenceValue = asset;
        serializedObject.ApplyModifiedProperties();
    }
    
    public static IEnumerable<Type> GetAllDerivedTypes(Type baseType)
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                types = e.Types;
            }

            if (types == null) continue;

            foreach (Type type in types)
            {
                if (type == null) continue;

                if (!type.IsAbstract && baseType.IsAssignableFrom(type))
                {
                    yield return type;
                }
            }
        }
    }
}
