using UnityEngine;

using Random = UnityEngine.Random;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class WanderingBoundsBox : WanderingBounds
{
    public Bounds Bounds = new Bounds( Vector3.zero, Vector3.one * 5 );

    public override Vector3 GetRandomLocationInBounds( Vector3 distribution )
    {
        // Random point within unit box
        var x = Random.Range( -1F, +1F ) * distribution.x;
        var y = Random.Range( -1F, +1F ) * distribution.y;
        var z = Random.Range( -1F, +1F ) * distribution.z;

        // Scale unit box to real bounds
        x = Bounds.center.x + Bounds.extents.x * z;
        y = Bounds.center.y + Bounds.extents.y * z;
        z = Bounds.center.z + Bounds.extents.z * z;

        // Return vector
        return new Vector3( x, y, z );
    }

    public override bool IsLocationInBounds( Vector3 location )
    {
        return Bounds.Contains( location );
    }

#if UNITY_EDITOR

    [DrawGizmo( GizmoType.Selected | GizmoType.Active )]
    static void DrawBounds( WanderingBoundsBox box, GizmoType gizmoType )
    {
        Gizmos.color = Color.magenta;

        var min = box.Bounds.min;
        var max = box.Bounds.max;

        // Draw box
        GizmoDrawBox( new[] {
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
            } );
    }

    static void GizmoDrawBox( Vector3[] points, int[] indices )
    {
        for( int i = 0; i < indices.Length; i += 2 )
        {
            var v1 = points[indices[i + 0]];
            var v2 = points[indices[i + 1]];
            Gizmos.DrawLine( v1, v2 );
        }
    }

    [CustomEditor( typeof( WanderingBoundsBox ) )]
    public class FishBehaviourEditor : Editor
    {
        bool AdjustBoundingBox;

        protected virtual void OnSceneGUI()
        {
            var wander = target as WanderingBoundsBox;

            Handles.BeginGUI();
            GUILayout.BeginArea( new Rect( Screen.width - 210, Screen.height - 65, 200, 50 ) );
            AdjustBoundingBox = GUILayout.Toggle( AdjustBoundingBox, "Adjust Wandering Bounds" );
            GUILayout.EndArea();
            Handles.EndGUI();

            // 
            if( AdjustBoundingBox )
            {
                EditorGUI.BeginChangeCheck();
                Vector3 newMaxBounds = Handles.PositionHandle( wander.Bounds.max, Quaternion.identity );
                Vector3 newMinBounds = Handles.PositionHandle( wander.Bounds.min, Quaternion.identity );

                if( EditorGUI.EndChangeCheck() )
                {
                    Undo.RecordObject( wander, "Change Bounds" );
                    wander.Bounds.size = ( newMaxBounds - newMinBounds );
                    wander.Bounds.min = newMinBounds;
                    wander.Bounds.max = newMaxBounds;
                }
            }
        }
    }
#endif
}