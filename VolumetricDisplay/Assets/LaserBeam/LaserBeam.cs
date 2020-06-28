using UnityEngine;
using VolumetricLines;

public class LaserBeam : MonoBehaviour
{
    [Header("Collisions")]

    [Tooltip("If enabled, the beam will be sized to fit within the TargetCollider")]
    public bool OnlyDrawWithinCollider;
    [Tooltip("Is only used if OnlyDrawWithinCollider is true")]
    public Collider TargetCollider;
    [Tooltip("Determined what the laser beam collides with")]
    public LayerMask CollidersLayerMask;

    [Header("Beam Parameters")]
    public bool IsVisible = true;
    public Color LaserColor = Color.red;
    public float BeamWidth = 0.3f;
    public float MaximumLength = 10.0f;
    [Tooltip("Distance per second.")]
    public float ParticleSpeed = 2.0f;
    [Tooltip("Fraction of beam width")]
    public float ParticleSize = 0.2f;
    [Tooltip("As a fraction of BeamWidth")]
    public float ParticleSpread = 0.5f;
    [Tooltip("Particles per distance")]
    public int ParticleDensity = 500;
    public float BeamSpotLightFactor = 1.25f;

    private BoxCollider _beamParticlesBounds;
    private VolumetricLineBehavior _beam;
    private ParticleSystem _beamParticles;
    private Light _beamSpotLight;


    private void Start()
    {
        _beamParticlesBounds = gameObject.AddComponent<BoxCollider>();
        _beamParticlesBounds.hideFlags = HideFlags.HideInInspector;

        var beamGo = (GameObject)Instantiate(Resources.Load("Beam"), transform);
        _beam = beamGo.GetComponent<VolumetricLineBehavior>();

        var beamParticlesGo = (GameObject)Instantiate(Resources.Load("Beam Noise"), transform);
        _beamParticles = beamParticlesGo.GetComponent<ParticleSystem>();

        var beamSpotlightGo = (GameObject)Instantiate(Resources.Load("Beam Spot Light"), transform);
        _beamSpotLight = beamSpotlightGo.GetComponent<Light>();
    }

    public void SetBeamVisibility(bool pIsBeamVisible)
    {
        _beam.gameObject.SetActive(pIsBeamVisible);
        _beamSpotLight.gameObject.SetActive(pIsBeamVisible);
        _beamParticles.gameObject.SetActive(pIsBeamVisible);
        _beamParticlesBounds.enabled = pIsBeamVisible;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = LaserColor;

        Gizmos.DrawLine(transform.position, transform.position + transform.forward * MaximumLength);
    }

    private void Update()
    {
        if (!IsVisible)
        {
            SetBeamVisibility(false);
            return;
        }

        RaycastHit hit;
        bool raycast;

        var startPosition = transform.position;
        var endPosition = transform.position + transform.forward * MaximumLength;

        // Update the startPosition and endPosition based on the Target Collider if only supposed to draw within
        if (OnlyDrawWithinCollider)
        {
            // Make sure the target collider is in the colliders layer mask
            var mask = 1 << TargetCollider.gameObject.layer;

            // do a forward raycast to see where the beam should start
            raycast = Physics.Raycast(transform.position, transform.forward, out hit, MaximumLength, mask);

            // If didn't hit or isn't the right collider, then stop
            if (!raycast || hit.collider != TargetCollider)
            {
                return;
            }

            startPosition = hit.point;
            // do a backwards raycast to see where the beam should end
            const float safety = 1f;
            var size = hit.collider.bounds.size;
            var distanceToOtherSide = 2 * Mathf.Max(size.x, Mathf.Max(size.y, size.z));
            var oppositePosition = startPosition + transform.forward * (distanceToOtherSide + safety);
            raycast = Physics.Raycast(oppositePosition, -transform.forward, out hit, MaximumLength + safety, mask);

            if (!raycast)
            {
                return;
            }

            endPosition = hit.point;
        }

        // check for collisions with objects
        const float epsilon = 0.01f;
        var rayLength = Vector3.Distance(startPosition, endPosition);
        raycast = Physics.Raycast(startPosition + transform.forward * epsilon, transform.forward, out hit, rayLength, CollidersLayerMask);

        if (raycast)
        {
            rayLength = hit.distance;
            endPosition = hit.point;

            var distanceToLight = Vector3.Distance(_beamSpotLight.transform.position, hit.point);
            _beamSpotLight.range = MaximumLength;
            _beamSpotLight.spotAngle = Mathf.Rad2Deg * 2.0f * Mathf.Tan(((BeamSpotLightFactor * BeamWidth) / 2.0f) / distanceToLight);
            _beamSpotLight.color = LaserColor;
            _beamSpotLight.cullingMask = CollidersLayerMask;
        }

        _beam.LineColor = LaserColor;
        _beam.LineWidth = BeamWidth;
        _beam.StartPos = transform.InverseTransformPoint(startPosition);
        _beam.EndPos = transform.InverseTransformPoint(endPosition);

        _beamParticlesBounds.center = Vector3.forward * (rayLength / 2 + Vector3.Distance(transform.position, startPosition));
        _beamParticlesBounds.size = new Vector3(BeamWidth * ParticleSpread, BeamWidth * ParticleSpread, rayLength);

        var particleCount = (int)(ParticleDensity * rayLength);
        var mainParticleSystem = _beamParticles.main;
        mainParticleSystem.startColor = LaserColor * 0.5f;
        mainParticleSystem.maxParticles = particleCount;
        mainParticleSystem.startSpeed = ParticleSpeed;
        mainParticleSystem.startSize = ParticleSize * BeamWidth;

        var meanParticleTransitTime = (rayLength / ParticleSpeed) / 2;
        var emission = _beamParticles.emission;
        emission.rateOverTime = particleCount / meanParticleTransitTime;

        var shape = _beamParticles.shape;
        shape.scale = new Vector3(_beamParticlesBounds.size.x, _beamParticlesBounds.size.y, _beamParticlesBounds.size.z);
        shape.length = _beamParticlesBounds.size.z;

        _beamParticles.gameObject.transform.localPosition = _beamParticlesBounds.center;
        var trigger = _beamParticles.trigger;
        trigger.SetCollider(0, _beamParticlesBounds);

        SetBeamVisibility(true);

        if (!raycast)
        {
            _beamSpotLight.gameObject.SetActive(false);
        }
    }
}
