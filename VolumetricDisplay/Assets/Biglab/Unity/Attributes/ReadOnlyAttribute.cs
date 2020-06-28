using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

#endif

public class ReadOnlyAttribute : PropertyAttribute
{
    public bool OnlyWhilePlaying { get; set; }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var attr = (ReadOnlyAttribute)attribute;

        // If playing and only disabled while playing
        if (attr.OnlyWhilePlaying)
        {
            GUI.enabled = !Application.isPlaying;
        }
        else
        {
            GUI.enabled = false; // or always disabled
        }

        EditorGUI.PropertyField(position, property, label, true);

        GUI.enabled = true;
    }
}
#endif