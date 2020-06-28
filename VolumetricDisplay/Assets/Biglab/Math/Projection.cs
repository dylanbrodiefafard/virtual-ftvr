using Biglab.Calibrations;
using Biglab.Extensions;

using UnityEngine;

namespace Biglab.Math
{
    public static class Projection
    {
        #region Dylan's Projection Matrix

        /// <summary>
        /// Dylan's Projection Matrix. <para/>
        /// Fits tightly by computing a sheered frustum to fit around a spheroid volumetric camera.
        /// Will return a fixed 6 DoF projection matrix if it is unable to fit properly.
        /// </summary>
        /// <param name="targetCamera">Transform of the camera to compute the projection matrix for.</param>
        /// <param name="vCameraT">Transform of the volumetric camera.</param>
        /// <param name="aspect">Aspect ratio of the fallback fixed projection matrix.</param>
        /// <param name="fitScale">A scaling factor for the fit. 1 is a perfect fit.</param>
        /// <param name="radius">Local radius of the volumetric camera.</param>
        /// <returns>A tightly fit projection matrix, or in the fallback case, a fixed 6 DoF one.</returns>
        public static Matrix4x4 ComputeDylansFittedProjectionMatrix(Transform targetCamera, Transform vCameraT, float aspect, float fitScale, float radius = 0.5f)
        {
            // First, compute the near plane distance
            var nearClipPlane = GetSpheroidOffset(targetCamera, targetCamera.forward, Vector3.back * radius * fitScale, vCameraT);

            // Secondly, compute the near plane offsets
            float left, right, bottom, top;

            if (!GetNearPlaneOffsets(targetCamera, vCameraT, fitScale * radius, nearClipPlane, out left, out right, out bottom, out top)
                || right < left || top < bottom || nearClipPlane < 0)
            {
                // If getting the near plane offsets
                // OR
                // If the projection is degenerate, then return a fixed projection
                return ComputeFixedProjectionMatrix(vCameraT, aspect);
            }

            // Thirdly, compute the far plane distance
            var farClipPlane = GetSpheroidOffset(targetCamera, targetCamera.forward, Vector3.forward * radius * fitScale, vCameraT);

            // Clamp the FoV based on human vision
            // clamp FoV horizontally. See: https://en.wikipedia.org/wiki/Human_eye#Field_of_view
            ClampOffsetsToFieldOfView(ref left, ref right, nearClipPlane, 150);
            // clamp FoV vertically. See: https://en.wikipedia.org/wiki/Human_eye#Field_of_view
            ClampOffsetsToFieldOfView(ref bottom, ref top, nearClipPlane, 110);

            // Compute projection matrix
            return PerspectiveOffCenter(left, right, bottom, top, nearClipPlane, farClipPlane);
        }

        /// <summary>
        /// Clamps near plane offsets to given field of view.
        /// </summary>
        /// <param name="offsetMin">The minimum offset</param>
        /// <param name="offsetMax">The maximum offset</param>
        /// <param name="near">The near plane distance</param>
        /// <param name="fieldOfView">The maximum field of view to clamp them to</param>
        private static void ClampOffsetsToFieldOfView(ref float offsetMin, ref float offsetMax, float near, float fieldOfView)
        {
            var maxOffset = near * Mathf.Tan(fieldOfView * Mathf.Deg2Rad / 2.0f);
            var midPoint = (offsetMax + offsetMin) / 2.0f;
            offsetMin = Mathf.Max(offsetMin, midPoint - maxOffset);
            offsetMax = Mathf.Min(offsetMax, midPoint + maxOffset);
        }

        private static bool GetNearPlaneOffsets(Transform targetCamera, Transform spheroid, float spheroidRadius, float nearPlaneDistance, out float left, out float right, out float bottom, out float top)
        {
            var localSpherePosition = targetCamera.InverseTransformPoint(spheroid.position);

            // Get the left and right offsets
            var spheroidPositionXZ = new Vector2(localSpherePosition.x, localSpherePosition.z);
            var spheroidRadiusXZ = spheroidRadius * Mathf.Max(spheroid.TransformVector(targetCamera.forward).magnitude, spheroid.TransformVector(targetCamera.right).magnitude);

            if (!GetProjectedOffsets(spheroidPositionXZ, spheroidRadiusXZ, nearPlaneDistance, out left, out right))
            {
                bottom = top = float.NaN;
                return false;
            }

            // The the bottom and top offsets
            var spheroidPositionYZ = new Vector2(localSpherePosition.y, localSpherePosition.z);
            var spheroidRadiusYZ = spheroidRadius * Mathf.Max(spheroid.TransformVector(targetCamera.forward).magnitude, spheroid.TransformVector(targetCamera.up).magnitude);

            return GetProjectedOffsets(spheroidPositionYZ, spheroidRadiusYZ, nearPlaneDistance, out bottom, out top);
        }

        /// <summary>
        /// Gets near plane offsets in projected 2D space using circle-circle intersections.
        /// </summary>
        /// <param name="projectedSpheroidPosition"></param>
        /// <param name="projectedSpheroidRadius"></param>
        /// <param name="nearPlaneDistance"></param>
        /// <param name="minOffset"></param>
        /// <param name="maxOffset"></param>
        /// <returns>True if successful, false otherwise</returns>
        private static bool GetProjectedOffsets(Vector2 projectedSpheroidPosition, float projectedSpheroidRadius, float nearPlaneDistance, out float minOffset, out float maxOffset)
        {
            var projectedMidpointPosition = projectedSpheroidPosition / 2;
            var projectedMidpointRadius = projectedSpheroidPosition.magnitude / 2;

            // Get the intersection points on the Right-Forward plane
            Vector2 p1, p2;
            var numIntersections = MathB.CircleCircleIntersectionPoints(projectedMidpointPosition, projectedMidpointRadius, projectedSpheroidPosition, projectedSpheroidRadius, out p1, out p2);

            if (numIntersections != 2)
            {
                minOffset = maxOffset = float.NaN;
                return false;
            }

            // Rescale intersections to the image plane
            var forward = new Vector2(0, 1);
            p1 = p1.normalized * (nearPlaneDistance / Mathf.Cos(Vector2.Angle(p1, forward) * Mathf.Deg2Rad));
            p2 = p2.normalized * (nearPlaneDistance / Mathf.Cos(Vector2.Angle(p2, forward) * Mathf.Deg2Rad));

            // Get the left and right offsets
            minOffset = p1.x < p2.x ? p1.x : p2.x;
            maxOffset = p1.x < p2.x ? p2.x : p1.x;

            return true;
        }

        /// <summary>
        /// Gets an offset on the surface of a spheroid.
        /// </summary>
        /// <param name="targetCamera">Transform of the target camera</param>
        /// <param name="cameraAxis">Local axis in camera space</param>
        /// <param name="localPosition">Local position in camera space</param>
        /// <param name="spheroid">Transform of the spheroid</param>
        /// <returns>The offset</returns>
        private static float GetSpheroidOffset(Transform targetCamera, Vector3 cameraAxis, Vector3 localPosition,
            Transform spheroid)
            => Vector3.Dot(spheroid.TransformPoint(Quaternion.Inverse(spheroid.rotation) * targetCamera.rotation * localPosition) - targetCamera.position, cameraAxis);

        #endregion

        #region Andrew's Projection Matrix

        /// <summary>
        /// Andrews Projection Matrix. <para/>
        /// Fits tightly by forcing the camera to look at the center of the volumetric camera and tightening FOV around the volume.
        /// </summary>
        public static Matrix4x4 ComputeAndrewsFittedProjectionMatrix(Transform targetCamera, Transform vCameraT, float aspect, float frustumScalingFactor)
        {
            // Bounding sphere ( the spheree camera volume )

            var boundingSphereDiameter = vCameraT.transform.lossyScale.MaxElement() * frustumScalingFactor;
            var boundingSphereRadius = boundingSphereDiameter / 2;

            var displacment = vCameraT.transform.position - targetCamera.position;

            var fieldOfView = 2 * Mathf.Asin(boundingSphereRadius / displacment.magnitude) * Mathf.Rad2Deg;
            var nearClipPlane = displacment.magnitude - boundingSphereRadius;
            var farClipPlane = displacment.magnitude + boundingSphereRadius;

            // When the matrix generated will be degenerate, return a fixed projection matrix
            if ((fieldOfView <= 0) || (farClipPlane < nearClipPlane) || (nearClipPlane < 0))
            {
                return ComputeFixedProjectionMatrix(vCameraT, aspect);
            }

            return Matrix4x4.Perspective(fieldOfView, aspect, nearClipPlane, farClipPlane);
        }

        #endregion

        /// <summary>
        /// Computes a window projection matrix for the given camera and window. Returns a fixed projection if 
        /// </summary>
        /// <param name="targetCamera"></param>
        /// <param name="window"></param>
        /// <param name="vCameraT"></param>
        /// <param name="aspect"></param>
        /// <returns></returns>
        public static Matrix4x4 ComputeWindowProjectionMatrix(Transform targetCamera, Vector3 windowPosition, Vector2 windowSize, Transform vCameraT, float aspect, float farClipPlane)
        {
            var displacement = windowPosition - targetCamera.position;
            var forwardDistance = Vector3.Dot(displacement, targetCamera.forward);

            if (forwardDistance < 0.000001f) { return ComputeFixedProjectionMatrix(vCameraT, aspect); }

            var nearClipPlane = forwardDistance;

            var rightDistance = Vector3.Dot(displacement, targetCamera.right);
            var upDistance = Vector3.Dot(displacement, targetCamera.up);

            var halfScreenWidth = windowSize.x / 2;
            var halfScreenHeight = windowSize.y / 2;

            var top = upDistance + halfScreenHeight; var bottom = upDistance - halfScreenHeight;
            var right = rightDistance + halfScreenWidth; var left = rightDistance - halfScreenWidth;
            return PerspectiveOffCenter(left, right, bottom, top, nearClipPlane, farClipPlane);
        }

        /// <summary>
        /// Fixed 6 DOF Projection Matrix. <para/>
        /// A standard projection matrix with 75deg FOV. <para/>
        /// This projection matrix is a fail-safe matrix, its a waste of pixel density, but it works in degenerate cases.
        /// </summary>
        public static Matrix4x4 ComputeFixedProjectionMatrix(Transform vCameraT, float aspect)
        {
            var scale = vCameraT.lossyScale.MaxElement();
            return Matrix4x4.Perspective(75F, aspect, 0.01F * scale, 10F * scale);
        }

        // See: http://ksimek.github.io/2013/08/13/intrinsic/
        //      http://kgeorge.github.io/2014/03/08/calculating-opengl-perspective-matrix-from-opencv-intrinsic-matrix
        //      https://www.mathworks.com/help/vision/ug/camera-calibration.html
        //      https://docs.opencv.org/2.4/modules/calib3d/doc/camera_calibration_and_3d_reconstruction.html
        //      https://stackoverflow.com/a/28547576
        public static Matrix4x4 ComputeProjectionMatrixFromIntrinsics(int width, int height, Vector2 f,
            Vector2 opticalCenter, float alpha, float near, float far)
        {
            var skew = f.y * Mathf.Tan(alpha);

            return new Matrix4x4()
            {
                m00 = 2 * f.x / width,
                m01 = 2 * skew / width,
                m02 = 2 * (opticalCenter.x / width) - 1,
                m11 = 2 * f.y / height,
                m12 = 2 * (opticalCenter.y / height) - 1,
                m22 = -(far + near) / (far - near),
                m23 = 2 * far * near / (near - far),
                m32 = -1
            };
        }

        /// <summary>
        /// Compute a projection matrix given the intrinsics parameters of a camera/projector and near/far planes.
        /// </summary>
        /// <param name="intrinsics">Intrinsic parameters of the camera/projector</param>
        /// <param name="near">Distance to near plane</param>
        /// <param name="far">Distance to far plane</param>
        /// <returns>Projection matrix</returns>
        public static Matrix4x4 ComputeProjectionMatrixFromIntrinsics(Intrinsics intrinsics, float near, float far)
            => ComputeProjectionMatrixFromIntrinsics(intrinsics.PixelWidth, intrinsics.PixelHeight,
                intrinsics.FocalLengths, intrinsics.OpticalCenter, intrinsics.SkewCoefficientAlpha, near, far);

        public static Vector3 UnProject(Vector3 position, Matrix4x4 projectionInverse)
        {
            var v = projectionInverse * new Vector4(position.x, position.y, position.z, 1f);
            return v / v.w;
        }

        public static void GetCameraProjectionProperties(Matrix4x4 pProjectionMatrix, out float nearClip,
            out float farClip, out float fieldOfView)
        {
            var y = pProjectionMatrix[1, 1];

            // Get the scaling parts of the matrix
            var c = pProjectionMatrix[2, 2];
            var d = pProjectionMatrix[2, 3];
            var farToNearClipRatio = (c + 1) / (c - 1);

            // Computer camera properties
            farClip = (d * (1 - farToNearClipRatio)) / (-2 * farToNearClipRatio);
            nearClip = farClip * farToNearClipRatio;
            fieldOfView = Mathf.Atan(1f / y) * 2 * Mathf.Rad2Deg;
        }

        public static Matrix4x4 PerspectiveOffCenter(float left, float right, float bottom, float top, float near,
            float far)
        {
            // Compute the parts of the matrix that depend on the dynamic near/far plane values
            var x = (2.0F * near) / (right - left);
            var y = (2.0F * near) / (top - bottom);
            var a = (right + left) / (right - left);
            var b = (top + bottom) / (top - bottom);

            // Compute the scaling parts of the matrix now
            var c = -(far + near) / (far - near);
            var d = -(2.0F * far * near) / (far - near);
            const float e = -1.0F;

            // Build and return the matrix
            return new Matrix4x4
            {
                [0, 0] = x,
                [0, 1] = 0,
                [0, 2] = a,
                [0, 3] = 0,
                [1, 0] = 0,
                [1, 1] = y,
                [1, 2] = b,
                [1, 3] = 0,
                [2, 0] = 0,
                [2, 1] = 0,
                [2, 2] = c,
                [2, 3] = d,
                [3, 0] = 0,
                [3, 1] = 0,
                [3, 2] = e,
                [3, 3] = 0
            };
        }
    }
}