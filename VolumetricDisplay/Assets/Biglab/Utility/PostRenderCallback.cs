using System;
using UnityEngine;
using UnityEngine.Events;

public sealed class PostRenderCallback : MonoBehaviour
{
    [SerializeField]
    private RenderCallback _onRender;

    public event Action Rendered;

    class RenderCallback : UnityEvent { }

    private void OnPostRender()
    {
        // Call unity style callback
        _onRender?.Invoke();

        // Call C# style callback
        Rendered?.Invoke();
    }
}
