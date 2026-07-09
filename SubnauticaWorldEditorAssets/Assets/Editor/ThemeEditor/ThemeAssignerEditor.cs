using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using SNCoreEditor.UI.Theme;
using SNCoreEditor.UI.Theme.Definitions.Components;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ThemeAssigner))]
public class ThemeAssignerEditor : Editor
{
    private SerializedProperty _graphicKey;
    private SerializedProperty _componentsThemeDefinitions;

    private List<string> graphicKeys;
    
    private void OnEnable()
    {
        _graphicKey = serializedObject.FindProperty("graphicKey");
        ReflectAvailableGraphicsKeys();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        if (graphicKeys == null || graphicKeys.Count == 0)
        {
            EditorGUILayout.HelpBox("No graphic keys found.", MessageType.Warning);
            return;
        }
        
        int currentIndex = graphicKeys.IndexOf(_graphicKey.stringValue);

        if (currentIndex < 0)
            currentIndex = 0;

        int newIndex = EditorGUILayout.Popup(
            "Graphic Key",
            currentIndex,
            graphicKeys.ToArray()
        );
        _graphicKey.stringValue = graphicKeys[newIndex];
        
        serializedObject.ApplyModifiedProperties();
    }

    private void ReflectAvailableGraphicsKeys()
    {
        graphicKeys = new List<string>
        {
            "Unknown"
        };
        IEnumerable<Type> types = UnityCustomEditorUtils.GetAllDerivedTypes(typeof(ComponentThemeDefinition));
        foreach (Type type in types)
        {
            ComponentThemeDefinition componentDef = (ComponentThemeDefinition) FormatterServices.GetUninitializedObject(type);
            graphicKeys.Add(componentDef.GraphicKey());
        }
    }
}
