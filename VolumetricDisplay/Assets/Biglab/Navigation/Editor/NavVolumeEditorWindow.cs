using UnityEditor;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace Biglab.Navigation
{
    public class NavVolumeEditorWindow : EditorWindow
    {
        private Bounds _bounds;

        private float _cellSize = 0.33F;
        private NavVolume.VisibilityOptions Options;

        [MenuItem("Window/Navigation Volume")]
        public static void OpenMenu()
        {
            var window = GetWindow<NavVolumeEditorWindow>();
            window.Show();
        }

        private void OnEnable()
        {
            SceneView.onSceneGUIDelegate += OnSceneGUI;
            Options = new NavVolume.VisibilityOptions();
            FitBoundsToScene();
        }

        private void OnDisable()
        {
            SceneView.onSceneGUIDelegate -= OnSceneGUI;
        }

        private void FitBoundsToScene()
        {
            // Fit bounds to whole scene by default
            _bounds = new Bounds();
            foreach (var renderer in FindObjectsOfType<MeshRenderer>())
            {
                _bounds.Encapsulate(renderer.bounds);
            }
        }

        private void OnGUI()
        {
            // [BOUNDS] //

            GUILayout.Label("Volume Bounds", EditorStyles.boldLabel);

            if (GUILayout.Button("Fit Bounds To Scene"))
            {
                FitBoundsToScene();
            }

            EditorGUILayout.Space();

            _bounds = EditorGUILayout.BoundsField(_bounds);

            _cellSize = EditorGUILayout.FloatField(_cellSize);

            GUILayout.Label("Visibility", EditorStyles.boldLabel);

            EditorGUILayout.Space();

            if (GUILayout.Button("Generate"))
            {
                var volume = NavVolume.CreateNavigationVolume(_bounds, _cellSize, Options);

                EditorUtility.DisplayProgressBar("Generating", "Saving Navigation Volume", 0.66F);

                // 
                var sceneName = $"{SceneManager.GetActiveScene().name}_NavVolume";
                SaveAssetToDisk($"Save {nameof(NavVolume)} Asset", "Choose where to save the asset", volume, sceneName);

                EditorUtility.ClearProgressBar();
            }
        }

        private static bool SaveAssetToDisk<T>(string title, string message, T obj, string fileName) where T : Object
        {
            var path = EditorUtility.SaveFilePanelInProject(title, fileName, "asset", message);
            if (string.IsNullOrWhiteSpace(path) == false)
            {
                var asset = AssetDatabase.LoadAssetAtPath<NavVolume>(path);
                if (asset == null)
                {
                    // Create new asset
                    AssetDatabase.CreateAsset(obj, path);
                    AssetDatabase.SaveAssets();
                }
                else
                {
                    // Overrite existing
                    EditorUtility.CopySerialized(obj, asset);
                }

                // Save and return success
                AssetDatabase.SaveAssets();
                return true;
            }
            else
            {
                // Cancelled saving asset
                return false;
            }
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            Handles.DrawWireCube(_bounds.center, _bounds.size);

            _bounds.min = Handles.PositionHandle(_bounds.min, Quaternion.identity);
            _bounds.max = Handles.PositionHandle(_bounds.max, Quaternion.identity);

            HandleUtility.Repaint();
        }
    }
}