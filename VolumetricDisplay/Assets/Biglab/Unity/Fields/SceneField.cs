using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;

#endif

/// <summary>
/// A special data type that represents a scene that can be used in builds. 
/// The default SceneAsset only works within the editor.
/// </summary>
[Serializable]
public class SceneField
{
    [SerializeField]
    private Object _asset;

    [SerializeField]
    private string _path = "";

    [SerializeField]
    private string _name = "";

    /// <summary>
    /// The full path of the scene ( to be used with <see cref="UnityEngine.SceneManagement.SceneManager.LoadScene"/> ).
    /// </summary>
    public string Path => _path;

    /// <summary>
    /// The name of the scene.
    /// </summary>
    public string Name => _name;

    /// <summary>
    /// Loads the referenced scene synchronously.
    /// </summary>
    public void Load(LoadSceneMode mode)
        => SceneManager.LoadScene(_path, mode);

    /// <summary>
    /// Loads the referenced scene asynchronously.
    /// </summary>
    public AsyncOperation LoadAsync(LoadSceneMode mode = LoadSceneMode.Single)
        => SceneManager.LoadSceneAsync(_path, mode);

#if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(SceneField))]
    public class SceneFieldPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, GUIContent.none, property);

            position.height = EditorGUIUtility.singleLineHeight;

            // 
            var asset = property.FindPropertyRelative(nameof(_asset));
            var path = property.FindPropertyRelative(nameof(_path));
            var name = property.FindPropertyRelative(nameof(_name));

            position = EditorGUI.PrefixLabel(position, label);

            if (asset != null)
            {
                EditorGUI.BeginChangeCheck();
                var value = EditorGUI.ObjectField(position, asset.objectReferenceValue, typeof(SceneAsset), false);

                // Move to next area
                position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                GUI.enabled = false;
                if (asset.objectReferenceValue == null)
                { EditorGUI.LabelField(position, "<no scene selected>"); }
                else
                { EditorGUI.LabelField(position, TruncatePath(path.stringValue)); }
                GUI.enabled = true;

                if (EditorGUI.EndChangeCheck())
                {
                    asset.objectReferenceValue = value;
                    if (asset.objectReferenceValue != null)
                    {
                        // Gets the full path to the scene asset
                        var scenePath = AssetDatabase.GetAssetPath(asset.objectReferenceValue);
                        path.stringValue = scenePath;

                        // Gets the "name" of the scene
                        var filename = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                        name.stringValue = ObjectNames.NicifyVariableName(filename);

                        //
                        AddSceneToBuildList(scenePath);
                    }
                }
            }

            EditorGUI.EndProperty();
        }

        private void AddSceneToBuildList(string scenePath)
            => EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[] { new EditorBuildSettingsScene(scenePath, true) }).GroupBy(x => x.path).Select(g => g.First()).ToArray();

        private string TruncatePath(string path)
        {
            const string prefix = "assets/";

            // Normalize to lowercase
            var lowerPath = path.ToLower();

            // If x starts with the prefix, remove it
            if (lowerPath.StartsWith(prefix))
            {
                return path.Substring(prefix.Length);
            }

            return path;
        }
    }

#endif
}