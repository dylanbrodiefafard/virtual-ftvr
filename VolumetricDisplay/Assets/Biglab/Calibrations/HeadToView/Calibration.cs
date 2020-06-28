using Biglab.Math;
using System;
using UnityEngine;

namespace Biglab.Calibrations.HeadToView
{
    [Serializable]
    public class Calibration
    {
        public Vector3 OffsetInView;
        public Quaternion ViewToHeadRotation;
        public float Error;

        public void Transform(Vector3 positionDisplay, Transform viewAnchor)
        {
            // Transform the position using the affine transformation
            viewAnchor.position = TransformPosition(positionDisplay);
            viewAnchor.rotation = TransformRotation(positionDisplay);
        }

        public void Transform(Vector3 positionDisplay, Quaternion rotationDisplay, Transform viewAnchor)
        {
            viewAnchor.position = TransformPosition(positionDisplay, rotationDisplay);
            viewAnchor.rotation = TransformRotation(rotationDisplay);
        }

        public void Transform(Transform displayAnchor, Transform viewAnchor)
        {
            Transform(displayAnchor.position, displayAnchor.rotation, viewAnchor);
        }

        /// <summary>
        /// Constructs an identity calibration.
        /// </summary>
        public static Calibration CreateIdentity()
        {
            return new Calibration(Vector3.zero, Quaternion.identity);
        }

        private Vector3 TransformPosition(Vector3 positionDisplay)
        {
            // Do all the work in spherical coordinates
            // offset.x fix
            var dist = new Vector2(positionDisplay.x, positionDisplay.z).magnitude;
            var gamma = Mathf.Asin(OffsetInView.x / dist);
            var theta = Mathf.Atan2(positionDisplay.z, positionDisplay.x) - gamma;

            // offset.y fix
            dist = new Vector3(positionDisplay.x * Mathf.Cos(gamma), positionDisplay.y,
                positionDisplay.z * Mathf.Cos(gamma)).magnitude;
            var psi = Mathf.Asin(OffsetInView.y / dist);
            var phi = Mathf.Acos(positionDisplay.y / dist) + psi;

            // offset.z fix
            dist = new Vector3(positionDisplay.x * Mathf.Cos(gamma), positionDisplay.y,
                       positionDisplay.z * Mathf.Cos(gamma)).magnitude * Mathf.Cos(psi);
            var radius = dist + OffsetInView.z; // TODO: Figure out if this is supposed to be + or - and why

            // convert from spherical to cartesian
            return MathB.SphericalToCartesian(radius, theta, phi);
        }

        private Quaternion TransformRotation(Vector3 positionDisplay)
        {
            // 3 DoF case requires computing the position first
            var position = TransformPosition(positionDisplay);
            // Assume they are looking at the display
            return Quaternion.LookRotation(-position.normalized, Vector3.up);
        }

        private Vector3 TransformPosition(Vector3 positionDisplay, Quaternion displayRotation)
        {
            var viewToDisplayRotation = TransformRotation(displayRotation);
            var offsetD = viewToDisplayRotation * OffsetInView;
            return positionDisplay - offsetD;
        }

        private Quaternion TransformRotation(Quaternion displayRotation)
        {
            // return displayRotation * Quaternion.Inverse(ViewToHeadRotation);
            return displayRotation * ViewToHeadRotation;
        }

        public Calibration(Vector3 offsetInView, Quaternion viewToHeadRotation, float error = 0)
        {
            OffsetInView = offsetInView;
            ViewToHeadRotation = viewToHeadRotation;
            Error = error;
        }

        public Calibration(Transform anchor, Transform viewpoint)
        {
            OffsetInView = viewpoint.InverseTransformPoint(anchor.position);
            ViewToHeadRotation = Quaternion.Inverse(anchor.rotation) * viewpoint.rotation;
            Error = 0;
        }
    }
}