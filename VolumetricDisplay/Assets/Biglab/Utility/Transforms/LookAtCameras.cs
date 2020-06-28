using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class LookAtCameras : MonoBehaviour
{
    /// <summary>
    /// Negates the forward vector to flip which side of the object looks at the camera.
    /// </summary>
    [Tooltip("Negates the forward vector to flip which side of the object looks at the camera.")]
    public bool FlipFront;

    private void OnWillRenderObject()
    {
        var forward = (Camera.current.transform.position - transform.position).normalized;
        if (FlipFront)
        {
            forward = -forward;
        }

        // Compute and assign rotation
        transform.rotation = Quaternion.LookRotation(forward);
    }
}
