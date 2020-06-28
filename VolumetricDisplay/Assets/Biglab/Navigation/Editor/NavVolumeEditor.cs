using UnityEditor;

using UnityEngine;

namespace Biglab.Navigation
{
    [CustomEditor(typeof(NavVolume))]
    public class NavVolumeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var volume = target as NavVolume;

            GUILayout.Label($"Cell Size: {volume.CellSize}");
            GUILayout.Label($"Bounds: {volume.Bounds}");
            EditorGUILayout.Space();
            GUILayout.Label($"Width: {volume.GridWidth}");
            GUILayout.Label($"Height: {volume.GridHeight}");
            GUILayout.Label($"Depth: {volume.GridDepth}");
            EditorGUILayout.Space();
            GUILayout.Label($"Visibility Positions: {volume.VisibilityPositions.Count}");
        }
    }
}