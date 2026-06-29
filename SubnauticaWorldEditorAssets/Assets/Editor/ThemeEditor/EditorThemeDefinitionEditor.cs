using System.Collections.Generic;
using System.IO;
using SNCoreEditor.UI.Theme.Definitions;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EditorThemeDefinition))]
[CanEditMultipleObjects]
public class EditorThemeDefinitionEditor : Editor 
{
    private SerializedProperty _themeNameLanguageKey;
    private SerializedProperty _regionThemes;

    private readonly Dictionary<Object, Editor> _cachedEditors = new Dictionary<Object, Editor>();

    private void OnEnable()
    {
        _themeNameLanguageKey = serializedObject.FindProperty("themeNameLanguageKey");
        _regionThemes = serializedObject.FindProperty("regionThemes");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(_themeNameLanguageKey);
        
        if (GUILayout.Button("Add New Region Theme"))
        {
            CreateAndAddRegionTheme();
        }
        EditorGUILayout.PropertyField(_regionThemes, new GUIContent("Region Themes"), false);
        
        if (_regionThemes.isExpanded)
        {
            EditorGUI.indentLevel++;

            for (int i = 0; i < _regionThemes.arraySize; i++)
            {
                SerializedProperty element = _regionThemes.GetArrayElementAtIndex(i);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                object obj = element.objectReferenceValue;
                string ElementPropertyText = obj is RegionThemeDefinition regionDef ? $"{regionDef.name} Theme" : "Unknown Theme";
                EditorGUILayout.PropertyField(element, new GUIContent($"({i + 1}) {ElementPropertyText}"));
                
                if(UnityCustomEditorUtils.ElementRemoveButton(_regionThemes, i)) break;
                
                EditorGUILayout.EndHorizontal();

                if (element.objectReferenceValue != null)
                {
                    if (!_cachedEditors.TryGetValue(element.objectReferenceValue, out Editor editor) || editor == null)
                    {
                        CreateCachedEditor(element.objectReferenceValue, null, ref editor);
                        _cachedEditors[element.objectReferenceValue] = editor;
                    }
                    EditorGUI.indentLevel++;
                    editor.OnInspectorGUI();
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
            }
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }
    
    private void CreateAndAddRegionTheme()
    {
        RegionThemeDefinition asset = CreateInstance<RegionThemeDefinition>();
        string baseDirectory = Path.GetDirectoryName(AssetDatabase.GetAssetPath(target));
        string path = EditorUtility.SaveFilePanelInProject(
            "Create Region Theme",
            "Base",
            "asset",
            "Enter a name for the new Region Theme, the path is ignored",
            baseDirectory
        );
        if (string.IsNullOrEmpty(path))// user canceled request
            return;
        
        string regionName = Path.GetFileNameWithoutExtension(path);
        asset.SetRegionName(regionName);
        UnityCustomEditorUtils.CreateAssetInRegionFolder(asset, baseDirectory, regionName);
        UnityCustomEditorUtils.AddAssetToArray(_regionThemes, asset, serializedObject);
    } 

    private void OnDisable()
    {
        foreach (Editor editor in _cachedEditors.Values)
        {
            DestroyImmediate(editor);
        }
        _cachedEditors.Clear();
    }
}
