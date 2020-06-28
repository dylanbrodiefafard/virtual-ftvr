using Biglab.Extensions;
using Biglab.IO.Serialization;
using System;
using System.IO;
using JetBrains.Annotations;
using UnityEngine;

namespace Biglab.Calibrations.TrackingToDisplay
{
    [Serializable]
    public class Calibration
    {
        public Matrix4x4 TrackerToDisplayTransformation;
        public Quaternion TrackerToDisplayRotation => TrackerToDisplayTransformation.ToRotation();
        public float Error;

        private Calibration()
        {
        }

        public Calibration(Transform trackingSpace, Transform physicalDisplaySpace)
        {
            // Compute ground truths
            var trackerToDisplayRotation = Quaternion.Inverse(physicalDisplaySpace.rotation) * trackingSpace.rotation;
            var trackerToDisplayTranslation = physicalDisplaySpace.InverseTransformPoint(trackingSpace.position);
            var trackerToDisplayScale = trackingSpace.lossyScale.Divide(physicalDisplaySpace.lossyScale);

            var trackerToDisplay =
                Matrix4x4.TRS(trackerToDisplayTranslation, trackerToDisplayRotation, trackerToDisplayScale);

            // Tracker to Display
            TrackerToDisplayTransformation = trackerToDisplay;
            Error = 0;
        }


        /// <summary>
        /// Constructs an identity calibration.
        /// </summary>
        public static Calibration CreateIdentity()
        {
            return new Calibration
            {
                TrackerToDisplayTransformation = Matrix4x4.identity,
                Error = int.MaxValue
            };
        }

        #region Transformations

        private Quaternion TransformRotation(Quaternion trackingRotation)
        {
            return TrackerToDisplayRotation * trackingRotation;
        }

        private Vector3 TransformPosition(Vector3 trackingPosition)
        {
            return TrackerToDisplayTransformation.MultiplyPoint3x4(trackingPosition);
        }

        /// <summary>
        /// Transforms a position in tracking space todisplay space.
        /// Use this function when using a tracking system that doesn't track rotation (orientation).
        /// </summary>
        /// <param name="positionTracking">The position in tracking space.</param>
        /// <param name="displayAnchor">The anchor in display space to be moved.</param>
        public void Transform(Vector3 positionTracking, Transform displayAnchor)
        {
            // Transform the position using the affine transformation
            displayAnchor.position = TransformPosition(positionTracking);
            // The rotation is not modified since the tracking system isn't updating the rotation.
        }

        /// <summary>
        /// Transforms a position and rotation in tracking space to display space.
        /// </summary>
        /// <param name="positionTracking">The tracked position.</param>
        /// <param name="rotationTracking">The tracked rotation.</param>
        /// <param name="displayAnchor">The anchor in display space to be moved.</param>
        public void Transform(Vector3 positionTracking, Quaternion rotationTracking, Transform displayAnchor)
        {
            displayAnchor.position = TransformPosition(positionTracking);
            displayAnchor.rotation = TransformRotation(rotationTracking);
        }

        /// <summary>
        /// Transforms the tracked transform to a transform in display space.
        /// </summary>
        /// <param name="trackingAnchor">The tracked transform.</param>
        /// <param name="displayAnchor">The anchor in display space to be moved.</param>
        public void Transform(Transform trackingAnchor, Transform displayAnchor)
        {
            Transform(trackingAnchor.position, trackingAnchor.rotation, displayAnchor);
        }

        #endregion

        public static Calibration LoadFromFile(string filepath)
        {
            var json = File.ReadAllText(filepath);
            return json.DeserializeJson<Calibration>();
        }

        public static void SaveToFile([NotNull] Calibration calibration, [NotNull] string filepath)
        {
            if (calibration == null)
            {
                throw new ArgumentNullException(nameof(calibration));
            }

            if (filepath == null)
            {
                throw new ArgumentNullException(nameof(filepath));
            }

            var json = calibration.SerializeJson(true);
            File.WriteAllText(Path.ChangeExtension(filepath, ".json"), json);
        }
    }
}