using System.Collections.Generic;

using UnityEngine;

using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Constructs an editor populated a list of scenes.
/// </summary>
[CreateAssetMenu]
public class SceneList : ScriptableObject
// Author: Christopher Chamberlain - 2018
{
    [SerializeField] private List<SceneField> _scenes;

    /// <summary>
    /// The collection of scenes.
    /// </summary>
    public IReadOnlyList<SceneField> Scenes => _scenes;

#if UNITY_EDITOR

    [CustomEditor(typeof(SceneList))]
    private class SceneListEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Separator();

            if (GUILayout.Button(new GUIContent("Apply as Scene Build List")))
            {
                UpdateEditorBuildList(target as SceneList);
            }
        }

        public static void UpdateEditorBuildList(SceneList data)
        {
            var scenes = data.Scenes.Select(scene => scene.Path);

            // Create a list ( holding existing build scenes )
            var buildList = new List<EditorBuildSettingsScene>();

            // Append the new scenes
            foreach (var scene in scenes.Where(path => !string.IsNullOrEmpty(path)))
            {
                var sceneToAdd = new EditorBuildSettingsScene(scene, true);
                buildList.Add(sceneToAdd);
            }

            // Write new scenes back to build
            EditorBuildSettings.scenes = buildList.ToArray();
        }
    }

#endif
}