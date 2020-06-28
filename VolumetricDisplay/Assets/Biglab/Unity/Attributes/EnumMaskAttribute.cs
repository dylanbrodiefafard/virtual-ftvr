using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

#endif

[AttributeUsage(AttributeTargets.Field)]
public class EnumMaskAttribute : PropertyAttribute
{
}

#if UNITY_EDITOR

[CustomPropertyDrawer(typeof(EnumMaskAttribute))]
public class EnumMaskPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var value = (Enum)fieldInfo.GetValue(property.serializedObject.targetObject);

        EditorGUI.BeginProperty(position, label, property);

        EditorGUI.BeginChangeCheck();

        // Show the field
        value = EditorGUI.EnumFlagsField(position, label, value);
        if (EditorGUI.EndChangeCheck())
        {
            fieldInfo.SetValue(property.serializedObject.targetObject, value);
        }

        EditorGUI.EndProperty();
    }
}

#endif