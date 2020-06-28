using UnityEngine;
using System;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;

#endif

/// <summary>
/// A means to select a single layer ( much like <see cref="LayerMask"/> ).
/// </summary>
[Serializable]
public struct Layer
// Author: Christopher Chamberlain - 2018
{
    [SerializeField, FormerlySerializedAs("value")]
    private int _value;

    public static implicit operator int(Layer layer)
        => layer._value;

    public static implicit operator Layer(int value)
        => new Layer {_value = value};


#if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(Layer))]
    private class LayerAttributeEditor : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var value = property.FindPropertyRelative(nameof(_value));
            value.intValue = EditorGUI.LayerField(position, label, value.intValue);
        }
    }

#endif
}