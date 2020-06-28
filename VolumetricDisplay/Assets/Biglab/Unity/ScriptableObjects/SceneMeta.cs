using UnityEngine;

[CreateAssetMenu]
public class SceneMeta : ScriptableObject
{
    [SerializeField] private SceneField _scene;

    [SerializeField] private string _title;

    public SceneField Scene => _scene;

    public string Title => _title;
}