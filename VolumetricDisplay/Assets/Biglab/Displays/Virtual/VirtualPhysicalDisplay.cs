using System.Collections;
using Biglab.Tracking;
using UnityEngine;

namespace Biglab.Displays.Virtual
{
    public class VirtualPhysicalDisplay : MonoBehaviour
    {
        public bool ComputeGroundTruth = true;

        public Vector3 PhysicalToVolumeTranslation = Vector3.zero;

        public Quaternion PhysicalToVolumeRotation = Quaternion.identity;

        public float PhysicalToVolumeScaleFactor = 1.0f;

        public Vector3 PhysicalToVolumeScale
            => Vector3.one * PhysicalToVolumeScaleFactor;

        public Matrix4x4 PhysicalToVolumeTransformation => Matrix4x4.TRS(PhysicalToVolumeTranslation, PhysicalToVolumeRotation, PhysicalToVolumeScale);

        #region MonoBehaviour

        private void Awake()
        {
            var virtualSubsystem = FindObjectOfType<VirtualDisplaySubsystem>();

            // If no virtual subsystem then stop processing
            if (virtualSubsystem == null)
            {
                return;
            }

            // Register with the virtual display subsystem
            virtualSubsystem.RegisterVirtualPhysicalDisplay(this);
        }

        private IEnumerator Start()
        {
            if (!ComputeGroundTruth)
            {
                yield break;
            }

            // Wait for tracking to be ready
            yield return TrackingSystem.Instance.GetWaitForSubsystem();

            // TODO: Test optitrack with virtual physical display

            // Create the Ground truth calibration from Tracking -> Physical
            var calibration = Calibrations.TrackingToDisplay.Calibration.CreateIdentity();
            if (TrackingSystem.Instance.TrackingSpace == null)
            {
                // Tracking space is Unity World, so just use the display's worldToLocalMatrix
                calibration.TrackerToDisplayTransformation = transform.worldToLocalMatrix;
            }
            else
            {
                // Use the calibration class to compute the ground truth
                calibration = new Calibrations.TrackingToDisplay.Calibration(TrackingSystem.Instance.TrackingSpace,
                    transform);
            }

            // Set the calibration in the tracking system
            TrackingSystem.Instance.SetCalibration(calibration);
        }

        #endregion MonoBehaviour
    }
}