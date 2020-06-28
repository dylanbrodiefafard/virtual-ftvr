using System.Collections;
using System.Linq;

using Biglab.Navigation;
using Biglab.Utility;

using UnityEngine;

using Random = UnityEngine.Random;

[SelectionBase]
public class HidingAI : MonoBehaviour
{
    public NavVolume NavVolume; 

    public MinMaxRange HidingThreshold = new MinMaxRange(0.1F, 0.4F);

    public float HidingClippingRadius = 5F;

    public float WaypointRadius = 0.5F;

    public MinMaxRange WaitingTime = new MinMaxRange(5, 15);

    public float Acceration = 0.03F;

    public float MaxSpeed = 10F;

    [Space]

    public GameObject BubblePrefab;

    public Light GlowLight;

    public float GlowIntensity = 10F;

    private float SpeedModifier
    {
        get
        {
            switch (_wandCount)
            {
                default:
                    return 1.0F;

                case 1:
                    return 0.15F;

                case 2:
                    return 0.0F;
            }
        }
    }

    [ReadOnly, SerializeField]
    private int _wandCount;

    [ReadOnly, SerializeField]
    private float _wandCaptureAmount;

    [SerializeField]
    private float _captureRate = 0.1F;

    private Rigidbody _rigidbody;

    private WaypointQueue _waypoints;

    private Vector3[] _hidingPositions;

    [Space]

    [ReadOnly, SerializeField]
    private Vector3 _currentHidingPosition;

    [ReadOnly, SerializeField]
    private State _state = State.NoPath;

    private Vector3 _lastFacingDirection;

    [Space]

    [SerializeField]
    private bool _renderVolumeGizmos = false;

    [Space]

    public AudioClip BubbleSFX;

    private Vector3 _homePosition;

    private bool _goHomeCommand = false;

    private enum State
    {
        Waiting,
        HasPath,
        NoPath
    }

    #region MonoBehaviour

    private void Start()
    {
        // Get components
        _rigidbody = GetComponent<Rigidbody>();

        // Find all hiding spots in nav volume
        _hidingPositions = NavVolume.GetPositions(CheckHidingPositionQualification).ToArray();

        // Record home position
        _homePosition = transform.position;

        // Teleport into a random position to start
        transform.position = _hidingPositions[Random.Range(0, _hidingPositions.Length)];

        // 
        ChooseNewDestination();

        // Create waypoint queue
        _waypoints = new WaypointQueue();
        _waypoints.ReachedEnd += () => StartCoroutine(HideAndWaitCoroutine());

        // Start with no path
        _state = State.NoPath;
    }

    public void GoHome()
    {
        _goHomeCommand = true;
        _currentHidingPosition = _homePosition;
        _state = State.NoPath;
    }

    private void Update()
    {
        // No path? Attempt to get a path.
        if (_state == State.NoPath)
        {
            // Put the agent in the waiting state
            if (_state != State.Waiting)
            {
                //Debug.Log($"({name}:{_state}) Waiting For Path");
                _state = State.Waiting;
            }

            // Find a new place to go
            if(!_goHomeCommand)
                ChooseNewDestination();

            // Trigger
            NavVolume.FindPath(transform.position, _currentHidingPosition, path =>
            {
                // Quick fix, if the path is requested but the fish is destroyed before completion.
                if (!this) return;

                // If we have a result?
                if (path != null)
                {
                    //Debug.Log($"({name}:{_state}) Found Path");

                    // Set waypoint and mark has path state
                    _waypoints.SetWaypoints(path.Select(co => NavVolume.GetPosition(co)));
                    _state = State.HasPath;
                }
                else
                {
                    //Debug.Log($"({name}:{_state}) No Path?");

                    // No path, mark no path state
                    _state = State.NoPath;
                }
            });
        }
        else
        {
            var distance = Vector3.Distance(transform.position, _waypoints.Target);
            var threshold = Mathf.Min(WaypointRadius, _rigidbody.velocity.magnitude);

            // If hiding, make "target area" bigger.
            if (_state == State.Waiting)
            {
                threshold = WaypointRadius * 3F;

                // Flattens rotation to level out
                if (!Mathf.Approximately(_lastFacingDirection.y, 0))
                {
                    _lastFacingDirection.y = 0;
                    _lastFacingDirection.Normalize();
                }
            }

            // If close enough claim the waypoint, setting the new target.
            if (distance < threshold)
            {
                //Debug.Log($"({name}:{_state}) Near Target");

                if (_state == State.HasPath)
                {
                    //Debug.Log($"({name}:{_state}) Claiming Target");
                    _waypoints.ClaimTargetWaypoint();
                }
            }
            // On the last waypoint, this is the also the behaviour when it waits around for the new path.
            else
            {
                //Debug.Log($"({name}:{_state}) Moving To Target");

                // Get the direction pointint at the waypoint target.
                // If its degenerate, just use the current forward vector.
                var dir = (_waypoints.Target - transform.position).normalized;
                if (Mathf.Approximately(dir.sqrMagnitude, 0))
                {
                    dir = transform.forward;
                }

                // Move towards target along the direction
                _rigidbody.velocity += dir * Acceration * SpeedModifier;

                // Compute speed limit
                var speedLimit = MaxSpeed * SpeedModifier;

                // Apply speed limit
                if (_rigidbody.velocity.magnitude > speedLimit)
                {
                    _rigidbody.velocity = _rigidbody.velocity.normalized * speedLimit;
                }

                // 
                _lastFacingDirection = dir;
            }

            // Look towards the lastest rotation
            _rigidbody.rotation = Quaternion.Slerp(_rigidbody.rotation, Quaternion.LookRotation(_lastFacingDirection), 0.1F);

            // If near the target, increase drag
            _rigidbody.drag = distance < 2 ? 1F : 0.2F;
        }

        // Glow
        GlowLight.intensity = _wandCaptureAmount * GlowIntensity;

        // Capturing
        if (_wandCount >= 2)
        {
            // 
            _wandCaptureAmount += _captureRate * Time.deltaTime;

            // 
            if (_wandCaptureAmount >= 1F)
            {
                // Poof!
                // Scheduler.StartCoroutine(ExplodeFish());
                ExplodeFish();
            }
        }
        else
        {
            _wandCaptureAmount -= _captureRate * (Time.deltaTime / 10F);
            _wandCaptureAmount = Mathf.Max(0, _wandCaptureAmount);
        }
    }

    #endregion

    void OnRaycastEnter(Raycaster caster)
    {
        var hit = caster.GetRaycastHit();
        Debug.Log($"Slowing: {hit.collider.name}");
        _wandCount++;
    }

    void OnRaycastExit(Raycaster caster)
    {
        _wandCount--;
    }

    private void ExplodeFish()
    {
        EmitBubbles(20);
        Destroy(gameObject);

        //// Hide
        //gameObject.SetActive(false);

        //// 
        //_wandCaptureAmount = 0;
        //_wandCount = 0;

        //// 
        //transform.position = _currentHidingPosition;
        //_state = State.NoPath;

        //yield return new WaitForSeconds(2F);

        //gameObject.SetActive(true);
    }

    private void EmitBubbles(int n)
    {
        AudioSource.PlayClipAtPoint(BubbleSFX, transform.position);

        for (var i = 0; i < n; i++)
        {
            var bubble = Instantiate(BubblePrefab);

            // Jitter
            var bubblePosition = transform.position;
            bubblePosition.x += Random.Range(-0.33F, +0.33F);
            bubblePosition.z += Random.Range(-0.33F, +0.33F);
            bubble.transform.position = bubblePosition;

            // Add explosion force
            var rb = bubble.GetComponent<Rigidbody>();
            rb.AddExplosionForce(100, transform.position, 2);
        }
    }

    private bool CheckHidingPositionQualification(NavVolume.Node node)
    {
        return (node.Visibility > HidingThreshold.Min && node.Visibility < HidingThreshold.Max)
            && (Vector3.Distance(NavVolume.Bounds.center, node.Position) < HidingClippingRadius);
    }

    private IEnumerator HideAndWaitCoroutine()
    {
        // Mark as waiting
        _state = State.Waiting;

        _goHomeCommand = false;

        // Wait some random amount of time
        var waitTime = WaitingTime.GetRandomValue();

        //Debug.Log($"({name}:{_state}) Waiting: {waitTime} seconds");

        yield return new WaitForSeconds(waitTime);

        //Debug.Log($"({name}:{_state}) Continuing");

        _state = State.NoPath;
    }

    private void ChooseNewDestination()
    {
        _currentHidingPosition = _hidingPositions[Random.Range(0, _hidingPositions.Length)];
        //Debug.Log($"({name}:{_state}) Choosing New Destination");
    }

    private void OnDrawGizmosSelected()
    {
        // Draw volume stuff
        if (_renderVolumeGizmos && NavVolume != null)
        {
            NavVolume.DrawGizmos(CheckHidingPositionQualification, true, false);

            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(NavVolume.Bounds.center, HidingClippingRadius);
        }

        // Draw waypoint path
        if (_waypoints != null)
        {
            Gizmos.color = Color.cyan;
            var previous = transform.position;
            foreach (var pt in _waypoints)
            {
                Gizmos.DrawLine(previous, pt);
                previous = pt;
            }
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, WaypointRadius);
        Gizmos.DrawWireSphere(_currentHidingPosition, WaypointRadius);

        if (_waypoints != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(_waypoints.Target, WaypointRadius / 2F);
        }
    }
}