using UnityEngine;

// Original source: http://www.unitygeek.com/unity_c_singleton/
// Extended with: http://answers.unity.com/answers/1275384/view.html

/// <summary>
/// A singleton monobehaviour. 
/// This class ensures the first instance existing in the scene is also the only.
/// </summary>
public abstract class SingletonMonobehaviour<T> : MonoBehaviour
    where T : SingletonMonobehaviour<T>
{
    private static T _instance;

    public static bool ApplicationIsQuitting { get; private set; }

    /// <summary>
    /// Gets the singleton instance ( creating it if it does not exist ).
    /// </summary>
    public static T Instance
    {
        get
        {
            // Is the instance is already set?
            if (_instance != null)
            {
                return _instance;
            }

            // Attempt to find an instance
            _instance = FindObjectOfType<T>();

            // Was the instance was found in the scene?
            if (_instance != null)
            {
                return _instance;
            }

            // If the application is quitting, stop here so we don't create a new GameObject on OnDestroy
            if (ApplicationIsQuitting)
            {
                return _instance;
            }

            // Create a new game object
            var instanceGameObject = new GameObject(typeof(T).Name);
            _instance = instanceGameObject.AddComponent<T>();

            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            // Store reference
            _instance = this as T;
        }
        else
        {
            // Purge the clone
            Destroy(gameObject);
        }
    }

    private void OnApplicationQuit()
        => ApplicationIsQuitting = true;

    // Remove reference
    private void OnDestroy()
        => _instance = null;
}