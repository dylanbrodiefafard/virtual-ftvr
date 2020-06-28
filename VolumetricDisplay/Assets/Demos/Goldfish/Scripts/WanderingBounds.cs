using UnityEngine;

public abstract class WanderingBounds : MonoBehaviour
{
    public abstract Vector3 GetRandomLocationInBounds( Vector3 distribution );

    public abstract bool IsLocationInBounds( Vector3 location );
}
