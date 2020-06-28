using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// A simple component to act like a comment field for prefabs/objects for the inspector.
/// </summary>
public class Comment : MonoBehaviour
// Author: Christopher Chamberlain - 2017
{
    [SerializeField, Tooltip("Write some notes.")]
    private string _comment;

#if UNITY_EDITOR

    [CustomEditor(typeof(Comment))]
    public class CommentEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var style = new GUIStyle(EditorStyles.textArea) { wordWrap = true };

            serializedObject.UpdateIfRequiredOrScript();

            EditorGUI.BeginChangeCheck();

            var comment = serializedObject.FindProperty(nameof(_comment));

            EditorGUILayout.Space();
            comment.stringValue = EditorGUILayout.TextArea(comment.stringValue, style);
            EditorGUILayout.Space();

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
    }

#endif
}