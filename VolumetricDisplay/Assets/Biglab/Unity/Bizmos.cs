using Biglab.Displays;
using Biglab.Math;
using UnityEngine;

using Stopwatch = System.Diagnostics.Stopwatch;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class Bizmos
{
    public static void DrawWireAxisSphere(Transform frame, float radius)
    {
        var startColor = Gizmos.color;

        Gizmos.DrawWireSphere(frame.position, radius);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(frame.position, frame.position + (frame.rotation * Vector3.forward).normalized * radius * 1.05f);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(frame.position, frame.position + (frame.rotation * Vector3.right).normalized * radius * 1.05f);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(frame.position, frame.position + (frame.rotation * Vector3.up).normalized * radius * 1.05f);

        Gizmos.color = startColor;
    }

    /// <summary>
    /// Draws for the given projection and view matrices.
    /// </summary>
    public static void DrawProjectionFrustum(Matrix4x4 proj, Matrix4x4 view)
    {
        var projInv = proj.inverse;
        var viewInv = view.inverse;

        // Draw near Plane
        var nearTopLeft = viewInv.MultiplyPoint3x4(Projection.UnProject(new Vector3(-1, 1, -1), projInv));
        var nearTopRight = viewInv.MultiplyPoint3x4(Projection.UnProject(new Vector3(1, 1, -1), projInv));
        var nearBottomLeft = viewInv.MultiplyPoint3x4(Projection.UnProject(new Vector3(-1, -1, -1), projInv));
        var nearBottomRight = viewInv.MultiplyPoint3x4(Projection.UnProject(new Vector3(1, -1, -1), projInv));

        Gizmos.DrawLine(nearTopLeft, nearTopRight);
        Gizmos.DrawLine(nearTopRight, nearBottomRight);
        Gizmos.DrawLine(nearBottomRight, nearBottomLeft);
        Gizmos.DrawLine(nearBottomLeft, nearTopLeft);

        // Draw far Plane
        var farTopLeft = viewInv.MultiplyPoint3x4(Projection.UnProject(new Vector3(-1, 1, 1), projInv));
        var farTopRight = viewInv.MultiplyPoint3x4(Projection.UnProject(new Vector3(1, 1, 1), projInv));
        var farBottomLeft = viewInv.MultiplyPoint3x4(Projection.UnProject(new Vector3(-1, -1, 1), projInv));
        var farBottomRight = viewInv.MultiplyPoint3x4(Projection.UnProject(new Vector3(1, -1, 1), projInv));

        Gizmos.DrawLine(farTopLeft, farTopRight);
        Gizmos.DrawLine(farTopRight, farBottomRight);
        Gizmos.DrawLine(farBottomRight, farBottomLeft);
        Gizmos.DrawLine(farBottomLeft, farTopLeft);

        // Connect the near and far plane
        Gizmos.DrawLine(farTopLeft, nearTopLeft);
        Gizmos.DrawLine(farTopRight, nearTopRight);
        Gizmos.DrawLine(farBottomRight, nearBottomRight);
        Gizmos.DrawLine(farBottomLeft, nearBottomLeft);
    }

    private static Stopwatch _progressSW = Stopwatch.StartNew();

    /// <summary>
    /// If within the editor, displays a progress bar window.
    /// </summary>
    public static void DisplayEditorProgress(string title, string description, float progress, bool timeRateLimit = true)
    {
#if UNITY_EDITOR
        if (!timeRateLimit || _progressSW.ElapsedMilliseconds > 50)
        {
            EditorUtility.DisplayProgressBar(title, description, progress);
            _progressSW.Restart();
        }
#endif
    }

    /// <summary>
    /// If within the editor, hides the progress bar window.
    /// </summary>
    public static void HideEditorProgress()
    {
#if UNITY_EDITOR
        EditorUtility.ClearProgressBar();
#endif 
    }

    /// <summary>
    /// Draws the Gizmos necessary to visualize the projection fitting.
    /// </summary>
    /// <param name="targetCamera">The camera to fit from</param>
    /// <param name="spheroidRadius">The radius of the spheroid in local space</param>
    public static void DrawDylanFrustumGizmo(Transform targetCamera, float spheroidRadius)
    {
        var spheroid = VolumetricCamera.Instance.transform;

        var localSpherePosition = targetCamera.InverseTransformPoint(spheroid.position);

        var spheroidPositionXZ = new Vector2(localSpherePosition.x, localSpherePosition.z);
        var spheroidRadiusXZ = spheroidRadius * Mathf.Max(spheroid.TransformVector(targetCamera.forward).magnitude, spheroid.TransformVector(targetCamera.right).magnitude);
        var midpointXZPosition = spheroidPositionXZ / 2;
        var midpointXZRadius = spheroidPositionXZ.magnitude / 2;


        var spheroidPositionYZ = new Vector2(localSpherePosition.y, localSpherePosition.z);
        var spheroidRadiusYZ = spheroidRadius * Mathf.Max(spheroid.TransformVector(targetCamera.forward).magnitude, spheroid.TransformVector(targetCamera.up).magnitude);
        var midpointYZPosition = spheroidPositionYZ / 2;
        var midpointYZRadius = spheroidPositionYZ.magnitude / 2;

        // Get the intersection points on the Right-Forward plane
        Vector2 p1, p2;
        MathB.CircleCircleIntersectionPoints(midpointXZPosition, midpointXZRadius, spheroidPositionXZ, spheroidRadiusXZ, out p1, out p2);

        var p1World = Quaternion.Inverse(targetCamera.rotation) * targetCamera.TransformPoint(new Vector3(p1.x, 0, p1.y));
        p1World.y = spheroid.position.y;
        var p2World = Quaternion.Inverse(targetCamera.rotation) * targetCamera.TransformPoint(new Vector3(p2.x, 0, p2.y));
        p2World.y = spheroid.position.y;

        Gizmos.color = Color.white;
        Gizmos.DrawCube(p1World, Vector3.one * 0.025f);
        Gizmos.DrawCube(p2World, Vector3.one * 0.025f);

        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(targetCamera.position, p1World);
        Gizmos.DrawLine(targetCamera.position, p2World);

        MathB.CircleCircleIntersectionPoints(midpointYZPosition, midpointYZRadius, spheroidPositionYZ, spheroidRadiusYZ, out p1, out p2);

        p1World = Quaternion.Inverse(targetCamera.rotation) * targetCamera.TransformPoint(new Vector3(0, p1.x, p1.y));
        p1World.x = spheroid.position.x;
        p2World = Quaternion.Inverse(targetCamera.rotation) * targetCamera.TransformPoint(new Vector3(0, p2.x, p2.y));
        p2World.x = spheroid.position.x;

        Gizmos.color = Color.white;
        Gizmos.DrawCube(p1World, Vector3.one * 0.025f);
        Gizmos.DrawCube(p2World, Vector3.one * 0.025f);

        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(targetCamera.position, p1World);
        Gizmos.DrawLine(targetCamera.position, p2World);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(spheroid.position, spheroidRadiusXZ);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(spheroid.position, spheroidRadiusYZ);
    }
}