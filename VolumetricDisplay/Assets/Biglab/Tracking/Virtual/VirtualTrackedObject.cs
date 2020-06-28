using System.Collections;
using Biglab.Calibrations.HeadToView;
using UnityEngine;

namespace Biglab.Tracking.Virtual
{
    public class VirtualTrackedObject : MonoBehaviour
    {
        public TrackedObjectKind ObjectKind;

        public Transform ObjectAnchor;
        public Transform LeftOrMonoEye;
        public Transform RightEye;

        public int ObjectIndex;

        public int Id => TrackedObject.GetTrackedObjectNumber(ObjectKind, ObjectIndex);

        public bool ShouldSetGroundTruth = true;

        private bool IsViewer => ObjectKind.Equals(TrackedObjectKind.Viewpoint);

        public CalibrationGroup GroundTruthCalibration { get; private set; }

        // State
        private bool _shouldOverride;

        #region MonoBehaviour

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.black;

            const float radius = 0.025f;
            Bizmos.DrawWireAxisSphere(transform, radius);

            if (ObjectKind == TrackedObjectKind.Viewpoint)
            {
                if (LeftOrMonoEye != null)
                {
                    Bizmos.DrawWireAxisSphere(LeftOrMonoEye, radius);
                }

                if (RightEye != null)
                {
                    Bizmos.DrawWireAxisSphere(RightEye, radius);
                }
            }
            else if(ObjectAnchor != null)
            {
                Bizmos.DrawWireAxisSphere(ObjectAnchor, radius);
            }
            
        }

        private void OnEnable()
        {
            GroundTruthCalibration = new CalibrationGroup(Calibration.CreateIdentity());
            if (IsViewer)
            {
                GroundTruthCalibration.AddCalibration(Calibration.CreateIdentity());
            }
        }

        private IEnumerator Start()
        {
            yield return null;
            // If a virtual tracking system, force calibration data 
            if (TrackingSystem.Instance.GetSubsystem() is VirtualSubsystem)
            {
                if (IsViewer)
                {
                    if (LeftOrMonoEye == null)
                    {
                        LeftOrMonoEye = new GameObject("Dummy Left Eye").transform;
                        LeftOrMonoEye.parent = transform;
                    }

                    if (RightEye == null)
                    {
                        RightEye = new GameObject("Dummy Right Eye").transform;
                        RightEye.parent = transform;
                    }
                }

                // Wait for tracking to be ready
                yield return TrackingSystem.Instance.GetWaitForSubsystem();

                if (IsViewer && ObjectIndex == 0)
                {
                    var ovrCameraRig = FindObjectOfType<OVRCameraRig>();

                    if (ovrCameraRig != null)
                    {
                        // tracking an ovr camera rig, so hook into its event system
                        ovrCameraRig.UpdatedAnchors += rig => OnGroundTruthChanged();
                    }
                }

                if (enabled)
                {
                    UpdateGroundTruthCalibration();
                }
            }
            else
            {
                Debug.LogWarning($"Virtual tracking subsystem was not found. Disabling {nameof(VirtualTrackedObject)} on {name}.");
                enabled = false;
            }
        }

        private void OnGroundTruthChanged()
        {
            if (enabled)
            {
                UpdateGroundTruthCalibration();
            }
        }

        /// <summary>
        /// Updates the calibrations of this virtual tracked object to match the current configuration of the object.
        /// </summary>
        public void UpdateGroundTruthCalibration()
        {
            if (IsViewer)
            {
                // Update eye calibrations
                GroundTruthCalibration.SetCalibration(0, new Calibration(transform, LeftOrMonoEye.transform));
                GroundTruthCalibration.SetCalibration(1, new Calibration(transform, RightEye.transform));
            }
            else
            {
                // Object calibration
                GroundTruthCalibration.SetCalibration(0, new Calibration(transform, ObjectAnchor == null ? transform : ObjectAnchor));
            }

            if (!_shouldOverride && ShouldSetGroundTruth)
            {
                TrackingSystem.Instance.SetCalibrations(ObjectKind, ObjectIndex, GroundTruthCalibration);
            }
        }

        #endregion

        public void CalibrationOverride(CalibrationGroup group)
        {
            TrackingSystem.Instance.SetCalibrations(ObjectKind, ObjectIndex, group);
            _shouldOverride = true;
        }

        public void ResetCalibrationOverride()
        {
            TrackingSystem.Instance.SetCalibrations(ObjectKind, ObjectIndex, GroundTruthCalibration);
            _shouldOverride = false;
        }
    }
}