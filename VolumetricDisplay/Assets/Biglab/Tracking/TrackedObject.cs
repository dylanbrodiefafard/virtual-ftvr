using System;
using System.Collections;

using Biglab.Calibrations.HeadToView;
using Biglab.Displays;

using UnityEngine;

namespace Biglab.Tracking
{
    /// <summary>
    /// Animates a transform in world space from a tracking space counterpart via the currently loaded calibration.
    /// </summary>
    public class TrackedObject : MonoBehaviour
    {
        public static event Action<TrackedObject> RegisteredTrackedObject;

        /// <summary>
        /// Siggraph Mapping
        /// ============================================
        /// ID  | Meaning           | Kind      | Index 
        /// ============================================
        /// 0   | Primary View (S)  | Viewer    | 0
        /// 1   | Primary View (M)  | Viewer    | 1
        /// 2   | Second  View (M)  | Viewer    | 2 
        /// 3   | Remote Viewer 1   | Object    | 0
        /// 4   | Remote Viewer 2   | Object    | 1
        /// 5   | Wand 1            | Object    | 2
        /// 6   | Wand 2            | Object    | 3
        /// 7   | Selfie Stick      | Object    | 4
        /// </summary>
        public const int MaximumViewpoints = 3;
        public const int MaximumTrackedObjects = 8;
        public const int MaximumObjects = MaximumTrackedObjects - MaximumViewpoints;

        public TrackedObjectKind ObjectKind = TrackedObjectKind.Viewpoint;

        public int ObjectIndex;

        public Transform TrackingAnchor
            => TrackingSystem.Instance.GetTrackingAnchor(ObjectKind, ObjectIndex);

        public CalibrationGroup Calibrations
        {
            get
            {
                var calibrations = TrackingSystem.Instance.GetCalibrationsWithFallback(ObjectKind, ObjectIndex);

                if (ObjectKind == TrackedObjectKind.Object || _viewer == null || _viewer.CanRenderStereo)
                {
                    return calibrations;
                }

                var leftCalibration = calibrations.GetCalibration(0);
                var rightCalibration = calibrations.GetCalibration(1);

                switch (_viewer.NonStereoFallbackEye)
                {
                    case Camera.MonoOrStereoscopicEye.Left:
                        rightCalibration = leftCalibration;
                        break;

                    case Camera.MonoOrStereoscopicEye.Right:
                        leftCalibration = rightCalibration;
                        break;

                    case Camera.MonoOrStereoscopicEye.Mono:
                        leftCalibration = rightCalibration = calibrations.GetAverageCalibration();
                        break;
                }

                // TODO: DF: It's technically possible to inject the view offset for window projection
                //  into the calibration for remote viewers
                // But it probably isn't the best place for it, because with full support I think we
                // will want to connect two viewers to create a proper window projection
                // I'm leaving this comment here in case we do decide to do something like this.
                //if (_viewer.Role == ViewerRole.Remote)
                //{
                //    // inject the window projection calibration
                //    rightCalibration = Calibration.CreateIdentity();
                //    rightCalibration.OffsetInView = Config.RemoteViewer.ViewInDevice;
                //    rightCalibration.ViewToHeadRotation = leftCalibration.ViewToHeadRotation;
                //}

                return new CalibrationGroup(leftCalibration, rightCalibration);
            }
        }

        private Viewer _viewer => GetComponent<Viewer>();

        public int ObjectId
        {
            get
            {
                // Gets the appropriate id for the specified kind and index
                return GetTrackedObjectNumber(ObjectKind, ObjectIndex);
            }

            set
            {
                // Set the kind and index
                ObjectIndex = GetTrackedObjectIndex(value);
                ObjectKind = GetTrackedObjectKind(value);
            }
        }

        #region MonoBehaviour

        private void OnDrawGizmosSelected()
        {
            var originalColor = Gizmos.color;
            

            if (_viewer == null)
            {
                Gizmos.color = Color.white;
                Bizmos.DrawWireAxisSphere(transform, 0.1f);
            }
            else
            {
                Gizmos.color = Color.red;
                Bizmos.DrawWireAxisSphere(_viewer.LeftAnchor, 0.1f);
                Gizmos.color = Color.green;
                Bizmos.DrawWireAxisSphere(_viewer.RightAnchor, 0.1f);
            }

            Gizmos.color = originalColor;
        }

        private IEnumerator Start()
        {
            yield return TrackingSystem.Instance.GetWaitForSubsystem();

            RegisteredTrackedObject?.Invoke(this);
        }

        private void Update()
        {
            // Wait for the tracker
            if (!TrackingSystem.Instance.IsReady) { return; }

            try
            {
                if (ObjectKind == TrackedObjectKind.Object)
                {
                    UpdateObjectAnchor(this, transform, transform);
                }
                else if (ObjectKind == TrackedObjectKind.Viewpoint && _viewer != null)
                {
                    UpdateViewerAnchors(this, _viewer);
                }
                else
                {
                    UpdateObjectAnchor(this, transform, transform);
                    Debug.LogWarning($"Tracked object is of kind {nameof(TrackedObjectKind.Viewpoint)}, but no {typeof(Viewer)} component was found. This anchor will be updated as a {nameof(TrackedObjectKind.Object)} instead.");
                }
            }
            catch (InvalidOperationException e) // No known subsystem id for tracked object
            {
                Debug.LogError(e);
                enabled = false;
            }
        }

        #endregion

        #region Anchor Manipulations

        public static void UpdateObjectAnchor(TrackedObject trackedObject, Transform trackingFrame, Transform calibratedFrame)
            => UpdateAnchors(trackedObject.Calibrations, trackedObject.TrackingAnchor, trackingFrame, calibratedFrame);

        public static void UpdateViewerAnchors(TrackedObject trackedObject, Viewer viewer)
            => UpdateAnchors(trackedObject.Calibrations, trackedObject.TrackingAnchor, viewer.transform, viewer.LeftAnchor, viewer.RightAnchor);

        public static void UpdateAnchors(CalibrationGroup calibrations, Transform trackingAnchor, Transform trackingFrame, Transform leftOrMono, Transform right = null)
        {
            // Transform the head anchor first incase the viewpoints are children of the transform
            TrackingSystem.Instance.TransformRelativeToVolumetricCamera(trackingAnchor, trackingFrame);

            // Transform the leftOrMono viewpoint
            TrackingSystem.Instance.TransformRelativeToVolumetricCamera(trackingAnchor, leftOrMono, calibrations.GetCalibration(0));

            // Transform the right viewpoint
            if (right != null && calibrations.Count == 2)
            { TrackingSystem.Instance.TransformRelativeToVolumetricCamera(trackingAnchor, right, calibrations.GetCalibration(1)); }
        }

        #endregion

        #region Kind/Index to Object Number (static)

        public static int GetTrackedObjectNumber(TrackedObjectKind kind, int index)
        {
            if (index < 0 || index > MaximumTrackedObjects - 1) { throw new ArgumentOutOfRangeException(nameof(index)); }

            var id = index;

            if (kind == TrackedObjectKind.Viewpoint)
            {
                if (index > MaximumViewpoints - 1) { throw new ArgumentOutOfRangeException(nameof(index)); }
            }
            else
            {
                if (index > MaximumObjects - 1) { throw new ArgumentOutOfRangeException(nameof(index)); }

                id += MaximumViewpoints;
            }

            return id;
        }

        /// <summary>
        /// Gets the kind of tracked object for the specified object id.
        /// </summary>
        public static TrackedObjectKind GetTrackedObjectKind(int id)
        {
            if (id < 0 || id > MaximumTrackedObjects - 1) { throw new ArgumentOutOfRangeException(nameof(id)); }

            if (id < MaximumViewpoints) { return TrackedObjectKind.Viewpoint; }

            if (id >= MaximumViewpoints && id < MaximumTrackedObjects) { return TrackedObjectKind.Object; }

            throw new ArgumentOutOfRangeException(nameof(id));
        }

        /// <summary>
        /// Gets the sub-index of whatever kind the tracked object is for the specified object id.
        /// </summary>
        public static int GetTrackedObjectIndex(int id)
        {
            if (id < 0 || id > MaximumTrackedObjects - 1) { throw new ArgumentOutOfRangeException(nameof(id)); }

            var kind = GetTrackedObjectKind(id);

            switch (kind)
            {
                case TrackedObjectKind.Viewpoint:
                    return id;
                case TrackedObjectKind.Object:
                    return id - MaximumViewpoints;
                default:
                    throw new ArgumentException($"Invalid kind: {kind} is not a valid enum.");
            }
        }

        #endregion
    }
}