using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class FishBehaviour : MonoBehaviour
{
    public Vector3 TargetPosition;
    public Vector3 Velocity;

    public float Speed = 1F;

    [Tooltip( "The minimum duration the fish will wait before selecting a new place to go." )]
    public float WaitingTimeMinimum = 4F;

    [Tooltip( "The maximum duration the fish will wait before selecting a new place to go." )]
    public float WaitingTimeMaximum = 12F;

    public float RotationInterpolationFactor = 0.04F;

    public WanderingBounds WanderingBounds;

    private Animator Animator;

    private Quaternion TargetRotation;

    private bool IsCloseEnough => Vector3.Distance( transform.position, TargetPosition ) < 1F;

    private float ChillTimer = 0F;

    private readonly Vector3 _wanderDistribution = Vector3.Normalize( new Vector3( 2, 1, 2 ) );

    void Start()
    {
        // Get bounds 
        Debug.Assert( WanderingBounds != null, "Fish must have an associated WanderingBounds component." );

        // Animate fish
        Animator = GetComponentInChildren<Animator>();
        Animator.Play( "Swim", 0, Random.Range( 0F, 1F ) );

        //
        ChooseNewPlaceToChill();
    }

    private float GetRandomWaitingTime()
    {
        return Random.Range( WaitingTimeMinimum, WaitingTimeMaximum );
    }

    bool RayCollide( Vector3 a, Vector3 b )
    {
        // 
        var dir = b - a;
        var dis = dir.magnitude;
        dir /= dis;

        // 
        var ray = new Ray( a, dir );
        // return Physics.Raycast( ray, dis );
        return Physics.SphereCast( ray, transform.localScale.x, dis );
    }

    void ChooseNewPlaceToChill()
    {
        // How long to 'chill out' before selecting a new place
        ChillTimer = GetRandomWaitingTime();

        // Randomly select a place that likely won't intersected target  
        TargetPosition = WanderingBounds.GetRandomLocationInBounds( _wanderDistribution );

        var limit = 10; // Collision avoidance
        while( RayCollide( transform.position, TargetPosition ) && ( --limit > 0 ) )
        {
            TargetPosition = WanderingBounds.GetRandomLocationInBounds( _wanderDistribution );
        }

        // 
        Velocity = Vector3.zero;
    }

    void Update()
    {
        if( IsCloseEnough )
        {
            // Randomly choose next target
            if( ChillTimer <= 0 )
            {
                ChooseNewPlaceToChill();
            }
            else
            {
                ChillTimer -= Time.deltaTime; // Count down
            }
        }
        else
        {
            var towardsTarget = ( TargetPosition - transform.position ).normalized;
            TargetRotation = Quaternion.LookRotation( towardsTarget );

            // 
            Velocity += towardsTarget * Speed * transform.localScale.x * 0.0005F;
            Velocity *= 0.99F;
        }

        transform.position += Velocity;
        Velocity *= 0.99F;

        // 
        transform.rotation = Quaternion.Lerp( transform.rotation, TargetRotation, RotationInterpolationFactor );
    }

    void FixedUpdate()
    {
        // 
        //if( RayCollide( transform.position, TargetPosition ) )
        //    ChooseNewPlaceToChill();
    }

    void OnTriggerEnter( Collider other )
    {
        ChooseNewPlaceToChill();
    }

#if UNITY_EDITOR
    [CanEditMultipleObjects]
    [CustomEditor( typeof( FishBehaviour ) )]
    public class FishBehaviourEditor : Editor
    {
        bool AdjustBoundingSphere;

        [DrawGizmo( GizmoType.InSelectionHierarchy | GizmoType.NotInSelectionHierarchy )]
        static void RenderCustomGizmo( Transform target, GizmoType gizmoType )
        {
            var fish = target.GetComponent<FishBehaviour>();
            if( fish == null )
            {
                return;
            }

            // Draws the fish's target
            var s = HandleUtility.GetHandleSize( fish.TargetPosition ) * 0.05F;
            DrawBox( fish.TargetPosition - Vector3.one * s, fish.TargetPosition + Vector3.one * s );

            var style = new GUIStyle();
            style.normal.textColor = Color.green;
            Handles.Label( fish.TargetPosition, fish.ChillTimer.ToString( "0.0" ), style );

            // Draw a line between here and there
            var collide = fish.RayCollide( fish.transform.position, fish.TargetPosition );
            Handles.color = collide ? Color.red : Color.white;
            Handles.DrawDottedLine( fish.transform.position, fish.TargetPosition, 4 );
        }

        private static void DrawBox( Vector3 min, Vector3 max )
        {
            Handles.DrawDottedLines( new[] {
                new Vector3( min.x, min.y, min.z ),
                new Vector3( max.x, min.y, min.z ),
                new Vector3( max.x, max.y, min.z ),
                new Vector3( min.x, max.y, min.z ),
                new Vector3( min.x, min.y, max.z ),
                new Vector3( max.x, min.y, max.z ),
                new Vector3( max.x, max.y, max.z ),
                new Vector3( min.x, max.y, max.z ),
            }, new int[] {
                0,1, 1,2,
                2,3, 3,0,

                4,5, 5,6,
                6,7, 7,4,

                0,4, 1,5,
                2,6, 3,7,
            }, 4 );
        }
    }
#endif
}
