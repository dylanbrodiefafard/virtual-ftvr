/// <summary>
/// A singleton monobehaviour with DontDestroyOnLoad. This ensures the first instance exsting in the scene is also the only.
/// </summary>
public abstract class ImmortalMonobehaviour<T> : SingletonMonobehaviour<T>
    where T : ImmortalMonobehaviour<T>
{
    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
    }
}