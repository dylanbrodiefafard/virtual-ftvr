using UnityEngine;

[RequireComponent(typeof(Projector))]
public class PerspectiveProjector : MonoBehaviour
{
    public Material ProjectorMaterial;

    public int IgnoreLayerMask;

    private void Awake()
    {
        var projector = gameObject.GetComponent<Projector>();
        projector.material = ProjectorMaterial;
        projector.orthographic = true;
        projector.ignoreLayers = IgnoreLayerMask;
    }
}