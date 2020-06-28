using System;
using System.Collections.Generic;
using System.Linq;

using Biglab.Displays;
using Biglab.Extensions;
using Biglab.IO.Networking;
using Biglab.Math;
using Biglab.Remote;
using Biglab.Tracking;

using UnityEngine;

namespace Biglab.Calibrations.HeadToView
{
    public class Calibrator : MonoBehaviour
    {
        /// <summary>
        /// A single measurement of a viewpoint fix.
        /// </summary>
        internal struct Sample
        {
            public Quaternion ViewToHeadRotationFix; // The rotation from viewpoint space to head-tracked space
            public Vector3 ViewToHeadTranslationfix; // The translation between viewpoint space to head-tracked space
            public float TimeCollected; // The time when the Sample was collected.
        }

        private const string realtimeCalibratorGroup = "Runtime Calibration";

        [Header("Tracked Object")]
        public TrackedObjectKind ObjectKind;
        public int ObjectIndex;

        public bool IsMeasuring { get; private set; }

        public bool IsCalibrating { get; private set; }

        public bool IsViewerCalibration => ObjectKind == TrackedObjectKind.Viewpoint;

        // Samples and state
        private List<Sample> _leftSamples;
        private List<Sample> _rightSamples;

        private List<Sample> _currentSamples =>
            _currentEye == Camera.MonoOrStereoscopicEye.Right
                ? _rightSamples
                : _leftSamples;

        private Camera.MonoOrStereoscopicEye _currentEye;

        private Camera.MonoOrStereoscopicEye _nextEye => IsViewerCalibration
            ? (_currentEye.Equals(Camera.MonoOrStereoscopicEye.Left)
                ? Camera.MonoOrStereoscopicEye.Right
                : Camera.MonoOrStereoscopicEye.Left)
            : Camera.MonoOrStereoscopicEye.Mono;

        private Transform _pausedEye =>
            _currentEye == Camera.MonoOrStereoscopicEye.Right
                ? _viewerToCalibrate.RightAnchor
                : _viewerToCalibrate.LeftAnchor;

        private Transform _liveEye =>
            _currentEye == Camera.MonoOrStereoscopicEye.Right
                ? _liveRightEyeAnchor
                : _liveLeftEyeAnchor;

        // Tracked objects
        private TrackedObject _trackedObjectToCalibrate;
        private Viewer _viewerToCalibrate;

        // Gameobjects to gather samples while the viewer is paused.
        private Transform _liveHeadAnchor;
        private Transform _liveLeftEyeAnchor;
        private Transform _liveRightEyeAnchor;
        private Transform _pausedObjectAnchor;

        // Remote menu
        private RemoteMenuDropdown _trackedObjectDropdown;

        private RemoteMenuButton _beginCalibrationButton;
        private RemoteMenuButton _finishCalibrationButton;
        private RemoteMenuButton _cancelCalibrationButton;

        private RemoteMenuButton _beginSampleButton;
        private RemoteMenuButton _finishSampleButton;
        private RemoteMenuButton _cancelSampleButton;

        private RemoteMenuLabel _calibrationStatus;

        #region MonoBehaviour

        private void OnDrawGizmos()
        {
            if (IsMeasuring)
            {
                if (IsViewerCalibration)
                {
                    var trackingAnchor = TrackingSystem.Instance.GetTrackingAnchor(ObjectKind, ObjectIndex);
                    TrackedObject.UpdateAnchors(_trackedObjectToCalibrate.Calibrations, trackingAnchor, _liveHeadAnchor, _liveLeftEyeAnchor, _liveRightEyeAnchor);
                }
                else
                {
                    TrackedObject.UpdateObjectAnchor(_trackedObjectToCalibrate, _liveHeadAnchor, _liveLeftEyeAnchor);
                }

                if (IsViewerCalibration)
                {
                    Gizmos.color = Color.green;
                    Bizmos.DrawWireAxisSphere(_liveLeftEyeAnchor, 0.12f);
                    Bizmos.DrawWireAxisSphere(_viewerToCalibrate.RightAnchor, 0.1f);
                    Gizmos.color = Color.blue;
                    Bizmos.DrawWireAxisSphere(_liveRightEyeAnchor, 0.12f);
                    Bizmos.DrawWireAxisSphere(_viewerToCalibrate.RightAnchor, 0.1f);
                }
                else
                {
                    Gizmos.color = Color.green;
                    Bizmos.DrawWireAxisSphere(_liveLeftEyeAnchor, 0.12f);
                    Gizmos.color = Color.blue;
                    Bizmos.DrawWireAxisSphere(_pausedObjectAnchor, 0.1f);
                }
            }
        }

        private void Start()
        {
            // Create and setup trackedobject dropdown
            _trackedObjectDropdown = gameObject.AddComponent<RemoteMenuDropdown>();
            _trackedObjectDropdown.Group = realtimeCalibratorGroup;
            _trackedObjectDropdown.Order = -1;

            _trackedObjectDropdown.ValueChanged += OnObjectKindChange;

            SetupTrackedObjectDropdown();

            // Create calibration action buttons
            _beginCalibrationButton = CreateButton("Begin Calibration", StartCalibration, true);
            _finishCalibrationButton = CreateButton("Finish Calibration", EndCalibration, false);
            // _cancelCalibrationButton = CreateButton("Cancel Calibration", CancelCalibration, true);
            _beginSampleButton = CreateButton("Start Sample", StartSampleCollection, false);
            _finishSampleButton = CreateButton("End Sample", EndSampleCollection, false);
            _cancelSampleButton = CreateButton("Cancel Sample", CancelSampleCollection, false);

            // Create calibration status label
            _calibrationStatus = gameObject.AddComponent<RemoteMenuLabel>();
            _calibrationStatus.Group = realtimeCalibratorGroup;
            _calibrationStatus.Order = gameObject.GetComponents<RemoteMenuButton>().Length + 1;
            _calibrationStatus.enabled = false;
        }

        #endregion

        private RemoteMenuButton CreateButton(string text, Action action, bool isEnabled)
        {
            var button = gameObject.AddComponent<RemoteMenuButton>();
            button.enabled = isEnabled;

            button.Group = realtimeCalibratorGroup;
            button.Order = gameObject.GetComponents<RemoteMenuButton>().Length;

            button.ValueChanged += (value, conn) => action();
            button.Text = text;

            return button;
        }

        // TODO: put this somewhere else, both calibrators use the identical code.. CODE DUPLICATION!!
        private void SetupTrackedObjectDropdown()
        {
            //
            var objectTypes = new List<string>();
            for (var i = 0; i < TrackedObject.MaximumTrackedObjects; i++)
            {
                var index = TrackedObject.GetTrackedObjectIndex(i);
                var kind = TrackedObject.GetTrackedObjectKind(i);
                objectTypes.Add($"{kind} {index}");
            }

            // Populate options
            _trackedObjectDropdown.Options = objectTypes;
        }

        // TODO: put this somewhere else, both calibrators use the identical code.. CODE DUPLICATION!!
        public void OnObjectKindChange(int index, INetworkConnection conn)
        {
            if (index < 0)
            {
                Debug.LogWarning("Somehow chose a negative tracking object number.");
                return;
            }

            // Set our object type
            ObjectKind = TrackedObject.GetTrackedObjectKind(index);
            ObjectIndex = TrackedObject.GetTrackedObjectIndex(index);
        }

        /// <summary>
        /// Gets a sample immediately. A sample is calculated from the difference between the paused coordinate frames and the current coordinate frames.
        /// </summary>
        /// <returns>Collects and returns a Sample immediately.</returns>
        internal Sample GetSample(Transform pausedViewpoint, Transform liveViewpoint)
            => new Sample
            {
                ViewToHeadRotationFix = Quaternion.Inverse(liveViewpoint.rotation) * pausedViewpoint.rotation,
                ViewToHeadTranslationfix = pausedViewpoint.InverseTransformPoint(liveViewpoint.position)
                    .Divide(DisplaySystem.Instance.PhysicalToWorld.ToScale()),
                TimeCollected = Time.time
            };


        /// <summary>
        /// Starts the sample collection.
        /// </summary>
        public void StartSampleCollection()
        {
            if (IsMeasuring)
            {
                return;
            }

            IsMeasuring = true;

            // Pause the updating of the tracked object
            _trackedObjectToCalibrate.enabled = false;

            if (IsViewerCalibration)
            {
                _viewerToCalibrate.NonStereoFallbackEye = _currentEye;
            }
            else
            {
                TrackedObject.UpdateObjectAnchor(_trackedObjectToCalibrate, _trackedObjectToCalibrate.transform, _pausedObjectAnchor);
                TrackedObject.UpdateObjectAnchor(_trackedObjectToCalibrate, _trackedObjectToCalibrate.transform, _trackedObjectToCalibrate.transform);
            }

            // 
            _beginSampleButton.enabled = false;
            _cancelSampleButton.enabled = true;
            _finishSampleButton.enabled = true;
        }

        /// <summary>
        /// Cancels the sample collection.
        /// </summary>
        public void CancelSampleCollection()
        {
            if (!IsMeasuring)
            {
                return;
            }

            IsMeasuring = false;
            _trackedObjectToCalibrate.enabled = true;

            // 
            _beginSampleButton.enabled = true;
            _cancelSampleButton.enabled = false;
            _finishSampleButton.enabled = false;
        }

        /// <summary>
        /// Collects the sample.
        /// </summary>
        public void EndSampleCollection()
        {
            if (!IsMeasuring)
            {
                return;
            }

            if (IsViewerCalibration)
            {
                var trackingAnchor = TrackingSystem.Instance.GetTrackingAnchor(ObjectKind, ObjectIndex);
                TrackedObject.UpdateAnchors(_trackedObjectToCalibrate.Calibrations, trackingAnchor, _liveHeadAnchor, _liveLeftEyeAnchor, _liveRightEyeAnchor);

                _currentSamples.Add(GetSample(_pausedEye, _liveEye));
            }
            else
            {
                TrackedObject.UpdateObjectAnchor(_trackedObjectToCalibrate, _liveHeadAnchor, _liveLeftEyeAnchor);

                _leftSamples.Add(GetSample(_pausedObjectAnchor, _liveLeftEyeAnchor));
            }

            IsMeasuring = false;
            _trackedObjectToCalibrate.enabled = true;

            _currentEye = _nextEye;

            //
            if (IsViewerCalibration)
            {
                UpdateStatusMessage("Current eye: " + (_currentEye == Camera.MonoOrStereoscopicEye.Left ? "Left" : "Right") + $" | {_leftSamples.Count + _rightSamples.Count} samples collected.");
            }
            else
            {
                UpdateStatusMessage($"{_leftSamples.Count} samples collected.");
            }

            // Do we have enough samples?
            if (!IsViewerCalibration && _leftSamples.Count >= 1 || IsViewerCalibration && _rightSamples.Count >= 1)
            {
                _finishCalibrationButton.enabled = true;
            }

            // 
            _beginSampleButton.enabled = true;
            _cancelSampleButton.enabled = false;
            _finishSampleButton.enabled = false;
        }

        /// <summary>
        /// Starts the calibration.
        /// </summary>
        public void StartCalibration()
        {
            foreach (var trackedObject in FindObjectsOfType<TrackedObject>())
            {
                if (trackedObject.ObjectKind != ObjectKind || trackedObject.ObjectIndex != ObjectIndex)
                {
                    continue;
                }

                _trackedObjectToCalibrate = trackedObject;
                break;
            }

            if (_trackedObjectToCalibrate == null)
            {
                Debug.LogWarning($"No tracked object with kind {ObjectKind} and index {ObjectIndex} was found in the scene");
                return;
            }

            if (IsViewerCalibration)
            {
                _viewerToCalibrate = _trackedObjectToCalibrate.GetComponent<Viewer>();
                _viewerToCalibrate.EnabledStereoRendering = false;
                _viewerToCalibrate.NonStereoFallbackEye = Camera.MonoOrStereoscopicEye.Left;
            }

            if (IsViewerCalibration && _viewerToCalibrate == null)
            {
                Debug.LogWarning($"Object kind was {ObjectKind}, but no component of type {nameof(Viewer)} was found on the tracked object");
                return;
            }

            // Disable tracked object dropdown
            _trackedObjectDropdown.enabled = false;

            // Enable status label
            _calibrationStatus.enabled = true;
            _calibrationStatus.Text = "";

            // Hide begin button, show cancel
            _beginCalibrationButton.enabled = false;
            // _cancelCalibrationButton.enabled = true;
            _beginSampleButton.enabled = true;

            _leftSamples = new List<Sample>();
            if (IsViewerCalibration)
            {
                _rightSamples = new List<Sample>();
            }

            IsMeasuring = false;
            IsCalibrating = true;

            // Create temporary game objects.
            _liveHeadAnchor = new GameObject("Live Head Anchor").transform;
            _liveLeftEyeAnchor = new GameObject("Live Left Viewpoint Anchor").transform;
            if (IsViewerCalibration)
            {
                _liveRightEyeAnchor = new GameObject("Live Right Viewpoint Anchor").transform;
            }
            else
            {
                _pausedObjectAnchor = new GameObject("Paused Object Anchor").transform;
            }

            _currentEye = IsViewerCalibration ? Camera.MonoOrStereoscopicEye.Left : Camera.MonoOrStereoscopicEye.Mono;
        }

        /// <summary>
        /// Ends the calibration.
        /// </summary>
        /// <exception cref="InvalidOperationException">No sample pairs have been collected.</exception>
        public void EndCalibration()
        {
            if (_leftSamples == null || IsViewerCalibration && _rightSamples == null)
            {
                Debug.LogWarning("You must start the calibration process before ending it.");
                return;
            }

            if (_leftSamples.Count < 1 || IsViewerCalibration && _rightSamples.Count < 1)
            {
                Debug.LogWarning($"You must collect more samples ({_leftSamples.Count}) than the minimum (1) before ending the calibration.");
                return;
            }

            var calibrations = TrackingSystem.Instance.GetCalibrations(ObjectKind, ObjectIndex);

            // Compute the fixes for the parameters
            Quaternion leftRotationFix;
            Vector3 leftTranslationFix;
            ComputeUpdatedParameters(_leftSamples, out leftRotationFix, out leftTranslationFix);

            Debug.Log($"Left calibration update: {leftRotationFix.eulerAngles.ToString("G4")}, translation fix: {leftTranslationFix.ToString("G4")}");

            // Get the current calibration
            var currentLeft = calibrations.GetCalibration(0);
            // Compute the fixes for the parameters
            var nextLeft = new Calibration(currentLeft.OffsetInView + leftTranslationFix, currentLeft.ViewToHeadRotation * leftRotationFix);
            // Compute difference with prior state
            var leftRotationalDelta = Quaternion.Angle(currentLeft.ViewToHeadRotation, nextLeft.ViewToHeadRotation);
            var leftPositionalDelta = Vector3.Distance(currentLeft.OffsetInView, nextLeft.OffsetInView);
            // Update the current calibrations with the fixes
            calibrations.SetCalibration(0, nextLeft);

            // Report calibration difference
            UpdateStatusMessage($"L {leftRotationalDelta}/{leftPositionalDelta}");

            if (IsViewerCalibration)
            {
                Quaternion rightRotationFix;
                Vector3 rightTranslationFix;
                ComputeUpdatedParameters(_rightSamples, out rightRotationFix, out rightTranslationFix);

                Debug.Log($"Right calibration update: {rightRotationFix.eulerAngles.ToString("G4")}, translation fix: {rightTranslationFix.ToString("G4")}");

                // Get the current calibration
                var currentRight = calibrations.GetCalibration(1);
                // Compute the fixes for the parameters
                var nextRight = new Calibration(currentRight.OffsetInView + rightTranslationFix, currentRight.ViewToHeadRotation * rightRotationFix);
                // Compute difference with prior state
                var rightRotationalDelta = Quaternion.Angle(currentRight.ViewToHeadRotation, nextRight.ViewToHeadRotation);
                var rightPositionalDelta = Vector3.Distance(currentRight.OffsetInView, nextRight.OffsetInView);
                // Update the current calibrations with the fixes
                calibrations.SetCalibration(1, nextRight);

                // Report calibration difference
                UpdateStatusMessage($"L {leftRotationalDelta}/{leftPositionalDelta} R {rightRotationalDelta}/{rightPositionalDelta}");

                _viewerToCalibrate.EnabledStereoRendering = true;
            }

            // Save the calibrations
            TrackingSystem.Instance.SetCalibrations(ObjectKind, ObjectIndex, calibrations);
            TrackingSystem.SaveCalibrations(ObjectKind, ObjectIndex, calibrations);

            // Destroy temporary game objects.
            if (_liveHeadAnchor != null)
            {
                Destroy(_liveHeadAnchor.gameObject);
            }

            if (_liveLeftEyeAnchor != null)
            {
                Destroy(_liveLeftEyeAnchor.gameObject);
            }

            if (_liveRightEyeAnchor != null)
            {
                Destroy(_liveRightEyeAnchor.gameObject);
            }

            if (_pausedObjectAnchor != null)
            {
                Destroy(_pausedObjectAnchor.gameObject);
            }

            IsCalibrating = false;

            _beginSampleButton.enabled = false;
            _finishCalibrationButton.enabled = false;
            _beginCalibrationButton.enabled = true;
            _trackedObjectDropdown.enabled = true;
        }

        private void UpdateStatusMessage(string text)
        {
            _calibrationStatus.enabled = true;
            _calibrationStatus.Text = text;
        }

        private static void ComputeUpdatedParameters(IReadOnlyCollection<Sample> samples, out Quaternion rotationFix,
            out Vector3 translationFix)
        {
            var numberOfSamples = samples.Count;
            var rotations = new List<Quaternion>(numberOfSamples);
            var offsets = new List<Vector3>(numberOfSamples);
            var weights = new List<float>(numberOfSamples); // TODO: make weights a parameter

            foreach (var sample in samples)
            {
                rotations.Add(sample.ViewToHeadRotationFix);
                offsets.Add(sample.ViewToHeadTranslationfix);
                weights.Add(1.0f / numberOfSamples);
            }

            rotationFix = MathB.ComputeMeanWeightedRotation(rotations, weights);

            translationFix = new Vector3()
            {
                x = offsets.WeightedAverage(vec => vec.x, vec => weights.ElementAt(offsets.IndexOf(vec))),
                y = offsets.WeightedAverage(vec => vec.y, vec => weights.ElementAt(offsets.IndexOf(vec))),
                z = offsets.WeightedAverage(vec => vec.z, vec => weights.ElementAt(offsets.IndexOf(vec)))
            };
        }
    }
}