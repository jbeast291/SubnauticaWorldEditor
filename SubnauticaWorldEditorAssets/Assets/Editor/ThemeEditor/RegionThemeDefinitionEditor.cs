using System;
using System.Collections.Generic;
using System.IO;
using SNCoreEditor.UI.Theme.Definitions;
using SNCoreEditor.UI.Theme.Definitions.Components;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RegionThemeDefinition))]
[CanEditMultipleObjects]
public class RegionThemeDefinitionEditor : Editor
{
    private SerializedProperty _regionName;
    private SerializedProperty _componentsThemeDefinitions;

    private void OnEnable()
    {
        _regionName = serializedObject.FindProperty("regionName");
        _componentsThemeDefinitions = serializedObject.FindProperty("componentsThemeDefinitions");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.PropertyField(_regionName);
        }
        EditorGUILayout.PropertyField(_componentsThemeDefinitions, new GUIContent("Component Theme Definitions"), false);

        if (_componentsThemeDefinitions.isExpanded)
        {
            EditorGUI.indentLevel++;

            for (int i = 0; i < _componentsThemeDefinitions.arraySize; i++)
            {
                SerializedProperty element = _componentsThemeDefinitions.GetArrayElementAtIndex(i);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                object obj = element.objectReferenceValue;
                string ElementPropertyText = obj != null ? $"{obj.GetType().Name}" : $"Element"; 
                EditorGUILayout.PropertyField(element, new GUIContent($"({i + 1}) {ElementPropertyText}"));

                if(UnityCustomEditorUtils.ElementRemoveButton(_componentsThemeDefinitions, i)) break;

                EditorGUILayout.EndHorizontal();

                if (element.objectReferenceValue != null)
                {
                    EditorGUILayout.Space(2);
                    SerializedObject so = new SerializedObject(element.objectReferenceValue);
                    so.Update();

                    SerializedProperty iterator = so.GetIterator();
                    iterator.NextVisible(true);

                    while (iterator.NextVisible(false))
                    {
                        EditorGUILayout.PropertyField(iterator, true);
                    }
                    so.ApplyModifiedProperties();
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space(10);

        if (GUILayout.Button("Add Component Theme Definition"))
        {
            ShowCreateMenu();
        }

        serializedObject.ApplyModifiedProperties();
    }
    
    private void ShowCreateMenu()
    {
        GenericMenu menu = new GenericMenu();
        IEnumerable<Type> types = UnityCustomEditorUtils.GetAllDerivedTypes(typeof(ComponentThemeDefinition));
        foreach (Type type in types)
        {
            menu.AddItem(new GUIContent(type.Name), false, () => CreateAndAdd(type));
        }
        menu.ShowAsContext();
    }
    
    private void CreateAndAdd(Type type)
    {
        string baseDirectory = Path.GetDirectoryName(AssetDatabase.GetAssetPath(target));
        ScriptableObject instance = CreateInstance(type);
        string assetPath = Path.Combine(baseDirectory, $"{target.name}{type.Name}.asset");
        UnityCustomEditorUtils.CreateAssetAt(instance, assetPath);
        UnityCustomEditorUtils.AddAssetToArray(_componentsThemeDefinitions, instance, serializedObject);
    }
}