using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using Biglab.Displays;
using Biglab.Extensions;
using Biglab.Tracking.Optitrack;
using Biglab.Tracking.Polhemus;
using Biglab.Tracking.Virtual;
using Biglab.Tracking.Fixed;

using UnityEngine;

using HVCalibration = Biglab.Calibrations.HeadToView.Calibration;
using HVCalibrations = Biglab.Calibrations.HeadToView.CalibrationGroup;
using TDCalibration = Biglab.Calibrations.TrackingToDisplay.Calibration;

namespace Biglab.Tracking
{
    public sealed class TrackingSystem : ImmortalMonobehaviour<TrackingSystem>
    {
        public Transform TrackingSpace => _subsystem.TrackingSpace;

        public bool IsReady => !_subsystem.IsNull() && _subsystem.IsReady;

        public bool Is6DoF => Config.GeneralTracker.Is6DoF;

        public event Action TrackingToPhysicalChanged;

        public TDCalibration TrackingToPhysical { get; private set; }

        private TrackingSubsystem _subsystem;

        private Dictionary<int, HVCalibrations> _calibrations;

        #region MonoBehaviour

        protected override void Awake()
        {
            base.Awake();

            _calibrations = new Dictionary<int, HVCalibrations>();

            TrackingToPhysical = GetCalibration();

            // Add tracking subsystem
            _subsystem = AddSubsystemComponent();
        }

        #endregion

        /// <summary>
        /// Returns the tracker subsystem.
        /// </summary>
        public TrackingSubsystem GetSubsystem()
            => _subsystem;

        /// <summary>
        /// A coroutine for waiting until the tracker subsystem is ready.
        /// </summary>
        public IEnumerator GetWaitForSubsystem()
        {
            yield return new WaitUntil(() => IsReady);
        }

        private TrackingSubsystem AddSubsystemComponent()
        // TODO: CC: TryCreateRenderer like DisplaySystem
        {
            var type = Config.GeneralTracker.GetTrackerType();

            Debug.Log($"Creating Tracking Subsystem: <b>{type}</b>");

            // 
            var subsystemGameObject = new GameObject();

            TrackingSubsystem subsystem;

            switch (type)
            {
                default:
                    throw new NotImplementedException($"Unknown tracking system {type}");

                case TrackingType.Optitrack:
                    subsystem = subsystemGameObject.AddComponent<OptitrackSubsystem>();
                    break;

                case TrackingType.Polhemus:
                    subsystem = subsystemGameObject.AddComponent<PolhemusSubsystem>();
                    break;

                case TrackingType.Virtual:
                    subsystem = subsystemGameObject.AddComponent<VirtualSubsystem>();
                    break;

                case TrackingType.Fixed:
                    subsystem = subsystemGameObject.AddComponent<FixedSubsystem>();
                    break;
            }

            // Set name and parent
            subsystemGameObject.transform.SetParent(gameObject.transform);
            subsystemGameObject.name = $"{subsystem.GetType().Name}";

            return subsystem;
        }

        #region Transformations

        /// <summary>
        /// Gets the position in tracking space of the anchor.
        /// </summary>
        /// <param name="position">The position to transform.</param>
        /// <returns>The position in tracking space of the anchor.</returns>
        public Vector3 GetTrackingPosition(Vector3 position)
            => TrackingSpace == null ? position : TrackingSpace.InverseTransformPoint(position);

        /// <summary>
        /// Gets the rotation in tracking space of the anchor.
        /// </summary>
        /// <param name="rotation">The rotation to transform.</param>
        /// <returns>The rotation in tracking space of the anchor.</returns>
        public Quaternion GetTrackingRotation(Quaternion rotation)
            => TrackingSpace == null ? rotation : Quaternion.Inverse(TrackingSpace.rotation) * rotation;

        /// <summary>
        /// Transforms the sourcePosition from Tracking to Physical space and sets the targetAnchor's position.
        /// </summary>
        /// <param name="sourcePosition">The source position in tracking space.</param>
        /// <param name="targetAnchor">The target anchor.</param>
        public void TransformRelativeToPhysical(Vector3 sourcePosition, Transform targetAnchor)
            => TrackingToPhysical.Transform(GetTrackingPosition(sourcePosition), targetAnchor);

        /// <summary>
        /// Transforms the position and rotation of trackingAnchor from Tracking to Physical space and sets the targetAnchor's rotation and position.
        /// </summary>
        /// <param name="sourceAnchor">The source anchor in tracking space.</param>
        /// <param name="targetAnchor">The target anchor.</param>
        public void TransformRelativeToPhysical(Transform sourceAnchor, Transform targetAnchor)
            => TrackingToPhysical.Transform(GetTrackingPosition(sourceAnchor.position),
                GetTrackingRotation(sourceAnchor.rotation), targetAnchor);

        /// <summary>
        /// Transforms the anchors position and rotation from Physical space to World space.
        /// </summary>
        /// <param name="anchor">The anchor to transform.</param>
        public void SetWorldFromPhysical(Transform anchor)
        {
            anchor.position = DisplaySystem.Instance.PhysicalToWorld.MultiplyPoint3x4(anchor.position);
            anchor.rotation = DisplaySystem.Instance.PhysicalToWorld.rotation * anchor.rotation;
        }

        /// <summary>
        /// Transforms the position and rotation of sourceAnchor (in Tracking space) relative to the volumetric Camera and sets the target anchor's position and rotation.
        /// This function will apply viewpoint offset/rotation correction if a headToView calibration is specified.
        /// This function will automatically transform the tracker anchor from local tracker space to world space if necessary.
        /// </summary>
        /// <param name="sourceAnchor">The rotation and position of this anchor will be used.</param>
        /// <param name="targetAnchor">The rotation and position of this anchor will be set.</param>
        /// <param name="headToView">The calibration to use for the viewpoint offset/rotation correction.</param>
        public void TransformRelativeToVolumetricCamera(Transform sourceAnchor, Transform targetAnchor,
            HVCalibration headToView = null)
        {
            if (sourceAnchor == null)
            {
                throw new ArgumentNullException(nameof(sourceAnchor));
            }

            if (targetAnchor == null)
            {
                throw new ArgumentNullException(nameof(targetAnchor));
            }

            if (Is6DoF) // Use 6 DoF calibration functions
            {
                // Tracker -> Physical
                TransformRelativeToPhysical(sourceAnchor, targetAnchor);

                // Head -> View correction
                headToView?.Transform(targetAnchor, targetAnchor);
            }
            else // Use 3 DoF calibration functions
            {
                // Tracker -> Physical
                TransformRelativeToPhysical(sourceAnchor.position, targetAnchor);

                // Head -> View correction
                headToView?.Transform(targetAnchor.position, targetAnchor);
            }

            // Physical -> Volumetric Camera -> World
            SetWorldFromPhysical(targetAnchor);
        }

        #endregion

        /// <summary>
        /// Gets the tracking anchor.
        /// </summary>
        /// <param name="kind">The kind of the tracked object.</param>
        /// <param name="index">The index of the tracked object.</param>
        /// <returns>The Transform that corresponds the the kind and index in the subsystem.</returns>
        /// <exception cref="InvalidOperationException">Will occur if the tracking subsystem doesn't have the specified kind and index.</exception>
        public Transform GetTrackingAnchor(TrackedObjectKind kind, int index)
        {
            // Look up object id
            var id = TrackedObject.GetTrackedObjectNumber(kind, index);

            // Find mapped index
            var subId = Array.FindIndex(Config.GeneralTracker.Mapping, x => x == id);

            // If index is null
            if (subId >= 0)
            {
                return _subsystem.GetTrackingAnchor(subId);
            }

            throw new InvalidOperationException($"No known subsystem id for tracked object {kind} {index}.");
        }

        #region Calibration Loading

        /// <summary>
        /// Attempts to get a reasonable calibration for the tracker object starting with the most expected, falling back to others.
        /// </summary>
        public HVCalibrations GetCalibrationsWithFallback(TrackedObjectKind kind, int index)
        {
            // Check dictionary
            var calibrations = GetCalibrations(kind, index);

            if (calibrations != null)
            {
                return calibrations;
            }

            // Fallback case 1: No calibration exists for TrackedObject, try and load it from disk
            calibrations = LoadCalibrations(kind, index);

            if (calibrations != null)
            {
                Debug.LogWarning($"No calibrations were set for {kind} {index}. Loaded calibrations from disk.");

                return SetCalibrations(kind, index, calibrations);
            }

            // Fallback case 2: No calibrations exist for TrackedObject, so create an identity calibration
            calibrations = kind == TrackedObjectKind.Viewpoint ?
                new HVCalibrations(HVCalibration.CreateIdentity(), HVCalibration.CreateIdentity()) :
                new HVCalibrations(HVCalibration.CreateIdentity());

            Debug.LogWarning($"No calibrations were set for {kind} {index}. Created identity calibrations.");

            return SetCalibrations(kind, index, calibrations);
        }

        /// <summary>
        /// Gets a loaded calibration for the given kind of tracker object.
        /// </summary>
        public HVCalibrations GetCalibrations(TrackedObjectKind kind, int index)
        {
            var id = TrackedObject.GetTrackedObjectNumber(kind, index);
            return _calibrations.ContainsKey(id) ? _calibrations[id] : null;
        }

        /// <summary>
        /// Stores a calibration for the given kind of tracker object.
        /// </summary>
        public HVCalibrations SetCalibrations(TrackedObjectKind kind, int index, HVCalibrations calibrations)
        {
            if (calibrations == null) { throw new ArgumentNullException(nameof(calibrations)); }

            // 
            var id = TrackedObject.GetTrackedObjectNumber(kind, index);
            _calibrations[id] = calibrations;

            return _calibrations[id];
        }


        /// <summary>
        /// Saves a tracking calibration for the given tracking object.
        /// </summary>
        public static void SaveCalibrations(TrackedObjectKind kind, int index, HVCalibrations calibrations)
        {
            var id = TrackedObject.GetTrackedObjectNumber(kind, index);
            var file = Path.Combine(Config.CalibrationPath, $"calibration_{id}.json");
            HVCalibrations.SaveToFile(calibrations, file);
        }

        /// <summary>
        /// Attempts to read a tracking calibration for the given tracking object.
        /// </summary>
        public static HVCalibrations LoadCalibrations(TrackedObjectKind kind, int index)
        {
            var id = TrackedObject.GetTrackedObjectNumber(kind, index);

            try
            {
                var file = Path.Combine(Config.CalibrationPath, $"calibration_{id}.json");
                return HVCalibrations.LoadFromFile(file);
            }
            catch (Exception)
            {
                Debug.LogWarning($"Unable to load calibration file for tracked object {kind} {index}");
                return null;
            }
        }

        public TDCalibration GetCalibration()
            => TrackingToPhysical ?? (TrackingToPhysical = LoadCalibration());

        public void SetCalibration(TDCalibration calibration)
        {
            TrackingToPhysical = calibration;
            TrackingToPhysicalChanged?.Invoke();
        }

        public static void SaveCalibration(TDCalibration calibration, string filepath = "calibration_tracker.json")
        {
            var file = Path.Combine(Config.CalibrationPath, filepath);
            TDCalibration.SaveToFile(calibration, file);
        }

        public static TDCalibration LoadCalibration(string filepath = "calibration_tracker.json")
        {
            try
            {
                var file = Path.Combine(Config.CalibrationPath, filepath);
                return TDCalibration.LoadFromFile(file);
            }
            catch (Exception)
            {
                Debug.LogWarning("Unable to load calibration file for tracking system. Using an identity calibration.");
                return TDCalibration.CreateIdentity();
            }
        }

        #endregion
    }
}