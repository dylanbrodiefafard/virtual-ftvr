using Biglab.Extensions;
using UnityEngine;

namespace Biglab.Calibrations.TrackingToDisplay
{
    public class Model : MonoBehaviour
    {
        [Header("Display")]
        [Tooltip("The transform that represents the coordinate frame of the physical display.")]
        public Transform DisplaySpace;

        [Header("Tracking")]
        [Tooltip("The transform that represents the coordinate frame of the tracker.")]
        public Transform TrackingSpace;

        [Space]
        [Tooltip("Which model mode to use.")]
        public ModelMode Mode = ModelMode.GroundTruth;

        public ParameterQuaternion TrackingToDisplayRotation;
        public ParameterVector3 TrackingToDisplayTranslation;

        public Vector3 TrackingToDisplayScale => TrackingSpace.lossyScale.Divide(DisplaySpace.lossyScale);
        // TODO: implement public Transform TrackingAnchorWithLatency { get; private set; }

        #region monobehaviour

        private void Awake()
        {
            TrackingToDisplayRotation = new ParameterQuaternion();
            TrackingToDisplayTranslation = new ParameterVector3();
        }

        private void Update()
        {
            if (DisplaySpace.IsNull() || TrackingSpace.IsNull())
            {
                return;
            }

            UpdateGroundTruthParameters();
        }

        #endregion monobehaviour

        public void UpdateGroundTruthParameters()
        {
            TrackingToDisplayRotation.GroundTruth = Quaternion.Inverse(DisplaySpace.rotation) * TrackingSpace.rotation;
            TrackingToDisplayTranslation.GroundTruth = DisplaySpace.InverseTransformPoint(TrackingSpace.position);
        }

        public void UpdateCalibration(Calibration calibration)
        {
            var translation = TrackingToDisplayTranslation.GetParameterValue(Mode);
            var rotation = TrackingToDisplayRotation.GetParameterValue(Mode);

            if (Mode.Equals(ModelMode.ErrorParameter))
            {
                translation += TrackingToDisplayTranslation.GetParameterValue(ModelMode.GroundTruth);
                rotation = TrackingToDisplayRotation.GetParameterValue(ModelMode.GroundTruth) * rotation;
            }

            calibration.TrackerToDisplayTransformation = Matrix4x4.TRS(translation, rotation, TrackingToDisplayScale);
        }
    }
}