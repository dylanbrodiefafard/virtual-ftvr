using UnityEngine;
using UnityEngine.Events;

public class Selectable : MonoBehaviour
{
    public UnityEvent OnSelectionEvent;
    public AudioClip OnSelectionSound;

    public void Awake()
    {
        if (OnSelectionEvent == null)
        {
            OnSelectionEvent = new UnityEvent();
        }
    }
    public void OnSelected()
    {
        if (OnSelectionSound != null)
        {
            AudioSource.PlayClipAtPoint(OnSelectionSound, transform.position);
        }

        OnSelectionEvent?.Invoke();
    }
}
