using UnityEngine;

public class BubbleBehaviour : MonoBehaviour
{
    private SphereCollider _collider;
    private Rigidbody _rigidbody;

    [Tooltip("Base speed for how quickly the bubble rises.")]
    public float FloatingSpeed = 1F;

    [Range(0F, 1F)]
    [Tooltip("Random variance on the floating speed.")]
    public float FloatingSpeedVariance = 0.1F;

    [Range(0F, 1F)]
    [Tooltip("Scale of the bubble.")]
    public float Scale = 0.8F;

    [Range(0F, 1F)]
    [Tooltip("Random variance on the scale of the bubble.")]
    public float ScaleVariance = 0.1F;

    private float _TargetScale;
    private float _CurrentScale;
    private float _Speed;

    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();

        _collider = GetComponent<SphereCollider>();
        _collider.enabled = false;

        // 
        _CurrentScale = Scale * (1 - ScaleVariance);
        _TargetScale = GetRandomScale();
        _Speed = GetRandomSpeed();

        // 
        Invoke("EnableCollider", 2F);
    }

    private float GetRandomSpeed()
    {
        var r = 1F - Random.Range(0F, FloatingSpeedVariance);
        return FloatingSpeed * r;
    }

    private float GetRandomScale()
    {
        var r = 1F - Random.Range(0F, ScaleVariance);
        return Scale * r;
    }

    void Update()
    {
        // Rise
        var velocity = _rigidbody.velocity;
        velocity.y += (_Speed * 0.03F) * (1F - Mathf.Pow(1F - transform.localScale.x, 2F));
        _rigidbody.velocity = velocity;

        // Computes a wobble
        var wx = Mathf.Sin(Time.time * _TargetScale * 3F);
        var wobble = new Vector3(1F + wx * 0.1F, 1F + wx * 0.1F, 1F);

        transform.localScale = wobble * _CurrentScale;
        _CurrentScale = Mathf.Lerp(_CurrentScale, _TargetScale, 0.01F);

        // Pop...?
        if (transform.position.magnitude > 6F) // 6 is a magic number for the clipping radius
        {
            Destroy(gameObject);
        }
    }

    void EnableCollider()
    {
        _collider.enabled = true;
    }

    void OnTriggerEnter(Collider other)
    {
        // Pop!
        // Debug.Log( other.name );
        // Destroy( gameObject );
    }
}
