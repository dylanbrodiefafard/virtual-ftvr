using UnityEngine;

using Random = UnityEngine.Random;

public class BubbleEmitter : MonoBehaviour
{
    public GameObject BubblePrefab;

    [Range( 0.1F, 60F )]
    public float Frequency = 3F;

    [Range( 0F, 1F )]
    public float FrequencyVariance = 0.2F;

    void Start()
    {
        // InvokeRepeating( "EmitBubble", Frequency, Frequency );
        Invoke( "EmitBubble", 0F );
    }

    private float GetFrequency()
    {
        var r = Random.Range( 0F, FrequencyVariance );
        return Frequency * ( 1F - r );
    }

    void Update()
    {

    }

    void EmitBubble()
    {
        var bubble = Instantiate( BubblePrefab, transform, false );

        // Jitter
        var bubble_Pos = bubble.transform.position;
        bubble_Pos.x += Random.Range( -1F, +1F );
        bubble_Pos.z += Random.Range( -1F, +1F );
        bubble.transform.position = bubble_Pos;

        Invoke( "EmitBubble", GetFrequency() );
    }
}
