using UnityEngine;

using Random = UnityEngine.Random;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class WanderingBoundsSphere : WanderingBounds
{
    public bool IsHemisphere = true;

    public Vector3 Center = Vector3.zero;

    public float Radius = 3F;

    public override Vector3 GetRandomLocationInBounds( Vector3 distribution )
    {
        // Random point within unit sphere
        var x = Random.Range( -1F, +1F ) * distribution.x;
        var y = Random.Range( -1F, +1F ) * distribution.y;
        var z = Random.Range( -1F, +1F ) * distribution.z;

        // Flip z if hemisphere
        if( IsHemisphere && y < 0 )
        {
            y = -y;
        }

        // Normalize point onto unit sphere shell
        var v = Vector3.Normalize( new Vector3( x, y, z ) );

        // Choose a point randomly along the radius
        v = v * Random.Range( 0F, 1F ) * Radius;
        return Center + v;
    }

    public override bool IsLocationInBounds( Vector3 location )
    {
        var distance = Vector3.Distance( Center, location );
        return distance <= Radius;
    }

#if UNITY_EDITOR

    [DrawGizmo( GizmoType.Selected | GizmoType.Active )]
    static void DrawBounds( WanderingBoundsSphere sphere, GizmoType gizmoType )
    {
        Gizmos.color = Color.magenta;

        // Draw sphere
        GizmoDrawArc( sphere.Center, Vector3.up, Vector3.forward, 360, sphere.Radius );

        for( float a = 0; a < 360; a += 90 )
        {
            var rot = Quaternion.AngleAxis( a, Vector3.up );
            GizmoDrawArc( sphere.Center, rot * Vector3.forward, Vector3.up, sphere.IsHemisphere ? 90 : 180, sphere.Radius );
        }
    }

    static void GizmoDrawArc( Vector3 center, Vector3 normal, Vector3 from, float angle, float radius )
    {
        from = from.normalized;

        var last = center + from * radius;
        for( float arc = 0; arc <= angle; arc += 10 )
        {
            var rot = Quaternion.AngleAxis( arc, normal );
            var vec = center + ( rot * from ) * radius;

            // Draw line
            Gizmos.DrawLine( last, vec );

            // 
            last = vec;
        }
    }

    [CustomEditor( typeof( WanderingBoundsSphere ) )]
    public class FishBehaviourEditor : Editor
    {
        bool AdjustBoundingSphere = false;

        protected virtual void OnSceneGUI()
        {
            var wander = target as WanderingBoundsSphere;

            // 
            Handles.BeginGUI();
            GUILayout.BeginArea( new Rect( Screen.width - 210, Screen.height - 65, 200, 50 ) );
            AdjustBoundingSphere = GUILayout.Toggle( AdjustBoundingSphere, "Adjust Wandering Bounds" );
            GUILayout.EndArea();
            Handles.EndGUI();

            // 
            if( AdjustBoundingSphere )
            {
                EditorGUI.BeginChangeCheck();
                var newCenter = Handles.PositionHandle( wander.Center, Quaternion.identity );
                var newRadius = Handles.RadiusHandle( Quaternion.identity, wander.Center, wander.Radius, true );

                // 
                if( EditorGUI.EndChangeCheck() )
                {
                    wander.Center = newCenter;
                    wander.Radius = newRadius;
                    Undo.RecordObject( wander, "Change Bounds" );
                }
            }
        }

        private static void DrawHemisphere( Vector3 center, float radius )
        {
            // Equator Ring
            Handles.DrawWireArc( center, Vector3.up, Vector3.forward, 360, radius );

            // 
            for( float a = 0; a < 360; a += 90 )
            {
                var q = Quaternion.AngleAxis( a, Vector3.up );
                var v = q * Vector3.forward;
                Handles.DrawWireArc( center, v, Vector3.up, 90, radius );
            }
        }

        private static void DrawSphere( Vector3 center, float radius )
        {
            // Equator Ring
            Handles.DrawWireArc( center, Vector3.up, Vector3.forward, 360, radius );

            // 
            for( float a = 0; a < 360; a += 90 )
            {
                var q = Quaternion.AngleAxis( a, Vector3.up );
                var v = q * Vector3.forward;
                Handles.DrawWireArc( center, v, Vector3.up, 180, radius );
            }
        }
    }
#endif
}
