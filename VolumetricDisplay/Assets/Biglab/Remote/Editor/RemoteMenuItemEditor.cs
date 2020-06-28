using UnityEditor;
using UnityEngine;

namespace Biglab.Remote.Editor
{
    [CustomEditor(typeof(RemoteMenuItem), true)]
    public class RemoteMenuItemEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            GUI.enabled = false;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
            GUI.enabled = true;

            var elementProperty = serializedObject.FindProperty("_element");
            elementProperty.NextVisible(true);

            do
            {
                EditorGUILayout.PropertyField(elementProperty, true);
            } while (elementProperty.NextVisible(false));

            EditorGUILayout.Space();

            if (ShouldShowEventProperty)
            {
                var onValueChangedProperty = serializedObject.FindProperty("_onValueChanged");
                EditorGUILayout.PropertyField(onValueChangedProperty);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private bool ShouldShowEventProperty => !(target is RemoteMenuLabel);
    }
}