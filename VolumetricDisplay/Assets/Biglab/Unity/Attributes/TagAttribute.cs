using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Marks a string as able to selected from the tags list.
/// </summary>
public class TagAttribute : PropertyAttribute { }

#if UNITY_EDITOR

[CustomPropertyDrawer(typeof(TagAttribute))]
public class SceneFieldPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType == SerializedPropertyType.String)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Display tag field
            property.stringValue = EditorGUI.TagField(position, label, property.stringValue);

            // If a blank values is given, set to untagged.
            if (string.IsNullOrEmpty(property.stringValue))
            {
                property.stringValue = "Untagged";
            }

            EditorGUI.EndProperty();
        }
        else
        {
            EditorGUI.PropertyField(position, property, label);
        }
    }
}

#endif