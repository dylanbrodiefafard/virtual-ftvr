using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;

#endif

// Source: http://answers.unity.com/answers/1314095/view.html

[AttributeUsage(AttributeTargets.Field)]
public class EulerAnglesAttribute : PropertyAttribute
{
}

#if UNITY_EDITOR

[CustomPropertyDrawer(typeof(EulerAnglesAttribute))]
public class EulerAnglesDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight * (EditorGUIUtility.wideMode ? 1f : 2f);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        Vector3 euler = property.quaternionValue.eulerAngles;
        EditorGUI.BeginProperty(position, label, property);
        EditorGUI.BeginChangeCheck();
        euler = EditorGUI.Vector3Field(position, label, euler);
        if (EditorGUI.EndChangeCheck())
        {
            property.quaternionValue = Quaternion.Euler(euler);
        }

        EditorGUI.EndProperty();
    }
}

#endif