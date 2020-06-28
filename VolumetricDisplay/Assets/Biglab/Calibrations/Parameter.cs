using System;
using Biglab.Math;
using UnityEngine;

namespace Biglab.Calibrations
{
    internal interface IParameter<out T>
    {
        void ComputeError();
        T GetParameterValue(ModelMode mode);
    }

    [Serializable]
    public class ParameterQuaternion : IParameter<Quaternion>
    {
        [SerializeField, EulerAngles] public Quaternion GroundTruth;
        [SerializeField, EulerAngles] public Quaternion Approximation;
        [SerializeField, EulerAngles] public Quaternion ErrorParameter;

        [SerializeField, ReadOnly] [Tooltip("Geodisic distance to align rotations in Degrees")]
        public float Error; /* Error of the guess */

        public ParameterQuaternion()
        {
            GroundTruth = Quaternion.identity;
            Approximation = Quaternion.identity;
            ErrorParameter = Quaternion.identity;
        }

        public void ComputeError()
        {
            Error = MathB.GeodesicDistanceBetweenRotations(GroundTruth, Approximation);
        }

        public Quaternion GetParameterValue(ModelMode mode)
        {
            switch (mode)
            {
                case ModelMode.GroundTruth:
                    return GroundTruth;
                case ModelMode.Approximation:
                    return Approximation;
                case ModelMode.ErrorParameter:
                    return ErrorParameter;
                default:
                    throw new ArgumentException(
                        $"{nameof(mode)} is not a valid mode for this parameter {typeof(ParameterQuaternion)}.");
            }
        }
    }

    [Serializable]
    public class ParameterVector3 : IParameter<Vector3>
    {
        [SerializeField] public Vector3 GroundTruth;
        [SerializeField] public Vector3 Approximation;
        [SerializeField] public Vector3 ErrorParameter;

        [SerializeField, ReadOnly] [Tooltip("Euclidean norm of displacement")]
        public float Error; /* Error of the guess */

        public ParameterVector3()
        {
            GroundTruth = Vector3.zero;
            Approximation = Vector3.zero;
            ErrorParameter = Vector3.zero;
        }

        public void ComputeError()
        {
            Error = Vector3.Distance(GroundTruth, Approximation);
        }

        public Vector3 GetParameterValue(ModelMode mode)
        {
            switch (mode)
            {
                case ModelMode.GroundTruth:
                    return GroundTruth;
                case ModelMode.Approximation:
                    return Approximation;
                case ModelMode.ErrorParameter:
                    return ErrorParameter;
                default:
                    throw new ArgumentException(
                        $"{nameof(mode)} is not a valid mode for this parameter {typeof(ParameterVector3)}.");
            }
        }
    }
}