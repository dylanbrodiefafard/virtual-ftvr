using UnityEngine;
using UnityEngine.Events;

public class RaycastSelector : MonoBehaviour
{
    public float MaxSelectionDistance = 100;

    public LayerMask SelectionLayerMask;

    public UnityEvent OnSelection;
    public UnityEvent OnPositiveSelection;
    public UnityEvent OnNegativeSelection;

    private void Awake()
    {
        OnSelection = new UnityEvent();
        OnPositiveSelection = new UnityEvent();
        OnNegativeSelection = new UnityEvent();
    }

    public RaycastHit CurrentHit => _currentHit;

    public Ray CurrentRay => new Ray(transform.position, transform.forward);

    private RaycastHit _currentHit;

    private void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.One)) { Select(); }
    }

    public void Select()
    {
        var didHit = Physics.Raycast(transform.position, transform.forward, out _currentHit, MaxSelectionDistance, SelectionLayerMask);

        OnSelection?.Invoke();

        if (!didHit)
        {
            OnNegativeSelection?.Invoke();
            return;
        }

        OnPositiveSelection?.Invoke();
        _currentHit.transform.SendMessage("OnSelected", SendMessageOptions.DontRequireReceiver);
    }
}
