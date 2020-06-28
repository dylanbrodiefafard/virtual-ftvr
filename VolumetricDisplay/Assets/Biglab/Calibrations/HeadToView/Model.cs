using Biglab.Extensions;
using UnityEngine;

namespace Biglab.Calibrations.HeadToView
{
    public class Model : MonoBehaviour
    {
        [Tooltip("The tracked point.")] public Transform Head;

        [Tooltip("The left (or mono) viewpoint.")]
        public Transform LeftOrMonoViewpoint;

        [Tooltip("The right (if stereo) viewpoint.")]
        public Transform RightViewpoint;

        [Space]
        [Tooltip("Which model mode to use.")]
        public ModelMode Mode = ModelMode.GroundTruth;

        public bool IsStereo = true;

        public ParameterQuaternion LeftOrMonoViewToHeadRotation;
        public ParameterQuaternion RightViewToHeadRotation;
        public ParameterVector3 LeftOrMonoOffsetInView;
        public ParameterVector3 RightOffsetInView;

        #region monobehaviour

        private void Awake()
        {
            LeftOrMonoViewToHeadRotation = new ParameterQuaternion();
            RightViewToHeadRotation = new ParameterQuaternion();
            LeftOrMonoOffsetInView = new ParameterVector3();
            RightOffsetInView = new ParameterVector3();
        }

        private void Update()
        {
            if (Head.IsNull() || LeftOrMonoViewpoint.IsNull() || RightViewpoint.IsNull())
            {
                return;
            }

            UpdateGroundTruthParameters();
        }

        #endregion monobehaviour

        public void UpdateGroundTruthParameters()
        {
            LeftOrMonoViewToHeadRotation.GroundTruth = Quaternion.Inverse(Head.rotation) * LeftOrMonoViewpoint.rotation;
            LeftOrMonoOffsetInView.GroundTruth = LeftOrMonoViewpoint.InverseTransformPoint(Head.position);
            RightOffsetInView.GroundTruth = RightViewpoint.InverseTransformPoint(Head.position);
            RightViewToHeadRotation.GroundTruth = Quaternion.Inverse(Head.rotation) * RightViewpoint.rotation;
        }

        public void UpdateCalibrations(Calibration left, Calibration right)
        {
            left.OffsetInView = LeftOrMonoOffsetInView.GetParameterValue(Mode);
            right.OffsetInView = RightOffsetInView.GetParameterValue(Mode);
            left.ViewToHeadRotation = LeftOrMonoViewToHeadRotation.GetParameterValue(Mode);
            right.ViewToHeadRotation = RightViewToHeadRotation.GetParameterValue(Mode);

            if (!Mode.Equals(ModelMode.ErrorParameter))
            {
                return;
            }

            left.OffsetInView += LeftOrMonoOffsetInView.GetParameterValue(ModelMode.GroundTruth);
            right.OffsetInView += RightOffsetInView.GetParameterValue(ModelMode.GroundTruth);
            left.ViewToHeadRotation = LeftOrMonoViewToHeadRotation.GetParameterValue(ModelMode.GroundTruth) *
                                      left.ViewToHeadRotation;
            right.ViewToHeadRotation = RightViewToHeadRotation.GetParameterValue(ModelMode.GroundTruth) *
                                       right.ViewToHeadRotation;
        }
    }
}