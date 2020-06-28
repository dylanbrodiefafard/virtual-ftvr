using System.Collections;
using System.Linq;

using UnityEngine;
using UnityEngine.SceneManagement;

using System.Collections.Generic;
using System.IO;

using Biglab.IO.Networking;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Biglab.Remote
{
    public class RemoteMenuSceneSwitcher : MonoBehaviour
    // TODO: CC: Maybe merge the dropdown inside this and exten RemoteMenuItem?
    {
        public SceneList SceneList
        {
            get { return _sceneList; }
            set { _sceneList = value; }
        }

        [SerializeField] private SceneList _sceneList;

        [SerializeField, ReadOnly]
        private RemoteMenuDropdown _sceneDropdown;

        [SerializeField, ReadOnly]
        private RemoteMenuButton _restartScene;

        private const string _sceneSelectionGroup = "Scene Selection";

        private IEnumerator Start()
        {
            yield return null;

            // Bind restart button
            _restartScene = gameObject.AddComponent<RemoteMenuButton>();
            _restartScene.Text = "Restart Current Scene";
            _restartScene.Group = _sceneSelectionGroup;
            _restartScene.Order = 1;

            _restartScene.ValueChanged += (_, c) =>
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().path, LoadSceneMode.Single);
            };

            // 
            _sceneDropdown = gameObject.AddComponent<RemoteMenuDropdown>();
            _sceneDropdown.ValueChanged += ValueChanged;

            var selected = -1;
            var options = new List<string>();
            for (var i = 0; i < GetSceneCount(); i++)
            {
                // TODO: Find appropriate scene meta file for enhanced title or other desired behaviours?
                var scenePath = GetScenePath(i);

                // Add names to options list
                options.Add(Path.GetFileNameWithoutExtension(scenePath));

                // If path is the same as the active scene, set index
                if (scenePath == GetActiveScenePath())
                {
                    selected = i;
                }
            }

            if (selected < 0)
            {
                Debug.LogWarning("Unable to find current scene in list.");
            }

            // Set scene list and select active scene by default
            _sceneDropdown.Options = options;
            _sceneDropdown.Selected = selected;

            // 
            _sceneDropdown.Group = _sceneSelectionGroup;
            _sceneDropdown.Order = 0;

            // Write scene list as guide text file
            var guideStr = "[THIS IS AUTO-GENERATED]\r\n";
            for (var i = 0; i < GetSceneCount(); i++)
            {
                var name = Path.GetFileNameWithoutExtension(GetScenePath(i));
                guideStr += $"{KeyCode.Keypad0 + i}: {name}\r\n";
            }

            File.WriteAllText("scene_guide.txt", guideStr);
        }

        private void ValueChanged(int index, INetworkConnection connection)
        {
            if (index < 0)
            {
                Debug.LogWarning("Somehow chose a negative scene index.");
                return;
            }

            // Load the new scene
            var scenePath = GetScenePath(index);
            SwitchScene(scenePath);
        }

        private void Update()
        {
            for (var i = 0; i < GetSceneCount(); i++)
            {
                if (Input.GetKeyDown(KeyCode.Keypad0 + i))
                {
                    var scenePath = GetScenePath(i);
                    SwitchScene(scenePath);
                    return;
                }
            }
        }

        private int GetSceneCount()
            => SceneList == null ? SceneManager.sceneCountInBuildSettings : SceneList.Scenes.Count;

        private string GetScenePath(int index)
            => SceneList == null ? SceneUtility.GetScenePathByBuildIndex(index) : SceneList.Scenes[index].Path;

        private static string GetActiveScenePath()
            => SceneManager.GetActiveScene().path;

        private static void SwitchScene(string path)
            => SceneManager.LoadScene(path, LoadSceneMode.Single);

#if UNITY_EDITOR

        [CustomEditor(typeof(RemoteMenuSceneSwitcher))]
        private class RemoteMenuSceneSwitcherEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                var switcher = target as RemoteMenuSceneSwitcher;

                if (switcher == null || !switcher._sceneDropdown)
                {
                    return;
                }

                var options = switcher._sceneDropdown.Options.ToArray();

                EditorGUILayout.Space();

                EditorGUI.BeginChangeCheck();
                var selected = EditorGUILayout.Popup(switcher._sceneDropdown.Selected, options);
                if (!EditorGUI.EndChangeCheck())
                {
                    return;
                }

                var scenePath = switcher.GetScenePath(selected);
                switcher._sceneDropdown.Selected = selected;
                SwitchScene(scenePath);
            }
        }

#endif
    }
}