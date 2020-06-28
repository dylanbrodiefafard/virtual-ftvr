using Biglab.Extensions;
using MathNet.Numerics.LinearAlgebra;
using UnityEngine;

namespace Biglab.Interoperability
{
    public static class MathNet
    {
        public static Quaternion ToQuaternion(Matrix<float> rotation)
        {
            var axisAngle = ToAxisAngle(rotation);
            var halfAngle = axisAngle.w / 2;
            var sinHalfAngle = Mathf.Sin(halfAngle);
            return new Quaternion
            {
                x = sinHalfAngle * axisAngle.x,
                y = sinHalfAngle * axisAngle.y,
                z = sinHalfAngle * axisAngle.z,
                w = Mathf.Cos(halfAngle)
            };
        }

        public static Vector4 ToAxisAngle(Matrix<float> rotation)
        {
            //angle = acos((trace(R) - 1) / 2);
            //if angle == 0
            //    axis = [0;1;0];
            //else
            //    d = sqrt((R(3, 2) - R(2, 3)) ^ 2 + (R(1, 3) - R(3, 1)) ^ 2 + (R(2, 1) - R(1, 2)) ^ 2);
            //    x = (R(3, 2) - R(2, 3)) / d;
            //    y = (R(1, 3) - R(3, 1)) / d;
            //    z = (R(2, 1) - R(1, 2)) / d;
            //axis = [x;y;z];
            var angle = System.Math.Acos((rotation.Trace() - 1) / 2);

            if (angle.AboutEquals(0))
            {
                return new Vector4(0, 1, 0, (float)angle);
            }

            var d = Vector3.Distance(new Vector3(rotation[2, 1], rotation[0, 2], rotation[1, 0]),
                new Vector3(rotation[1, 2], rotation[2, 0], rotation[0, 1]));

            var x = (rotation[2, 1] - rotation[1, 2]) / d;
            var y = (rotation[0, 2] - rotation[2, 0]) / d;
            var z = (rotation[1, 0] - rotation[0, 1]) / d;

            return new Vector4(x, y, z, (float)angle);
        }
    }
}