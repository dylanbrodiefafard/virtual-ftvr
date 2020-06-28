using UnityEngine;

namespace Biglab.Extensions
{
    public static class Matrix4x4Extension
    {

        public static Matrix4x4 NormalMatrix(this Matrix4x4 @this)
            => Matrix4x4.TRS(Vector3.zero, @this.ToRotation(), @this.ToScale().Inverse());

        /// <summary>
        /// Computes the mean between two TRS matrices
        /// </summary>
        /// <param name="this">this TRS matrix</param>
        /// <param name="other">the other TRS matrix</param>
        /// <returns>The average translation, rotation, and scale between the two TRS matrices.</returns>
        public static Matrix4x4 AffineMean(this Matrix4x4 @this, Matrix4x4 other)
        {
            var aT = @this.ToTranslation();
            var bT = other.ToTranslation();
            var aR = @this.ToRotation();
            var bR = other.ToRotation();
            var aS = @this.ToScale();
            var bS = @this.ToScale();

            return Matrix4x4.TRS((aT + bT) / 2.0f, Quaternion.Slerp(aR, bR, 0.5f), (aS + bS) / 2.0f);
        }

        /// <summary>
        /// Matrix and scalar multiplication.
        /// </summary>
        /// <param name="this">The matrix</param>
        /// <param name="scalar">The scalar</param>
        /// <returns>A matrix with every element multiplied by the scalar</returns>
        public static Matrix4x4 ScalarMultiplty(this Matrix4x4 @this, float scalar) => new Matrix4x4()
        {
            m00 = @this.m00 * scalar,
            m01 = @this.m01 * scalar,
            m02 = @this.m02 * scalar,
            m03 = @this.m03 * scalar,
            m10 = @this.m10 * scalar,
            m11 = @this.m11 * scalar,
            m12 = @this.m12 * scalar,
            m13 = @this.m13 * scalar,
            m20 = @this.m20 * scalar,
            m21 = @this.m21 * scalar,
            m22 = @this.m22 * scalar,
            m23 = @this.m23 * scalar,
            m30 = @this.m30 * scalar,
            m31 = @this.m31 * scalar,
            m32 = @this.m32 * scalar,
            m33 = @this.m33 * scalar
        };

        /// <summary>
        /// Element-wise addition of two matrices.
        /// </summary>
        /// <param name="this">The first matrix.</param>
        /// <param name="other">The second matrix</param>
        /// <returns></returns>
        public static Matrix4x4 Add(this Matrix4x4 @this, Matrix4x4 other) => new Matrix4x4()
        {
            m00 = @this.m00 + other.m00,
            m01 = @this.m01 + other.m01,
            m02 = @this.m02 + other.m02,
            m03 = @this.m03 + other.m02,
            m10 = @this.m10 + other.m10,
            m11 = @this.m11 + other.m11,
            m12 = @this.m12 + other.m12,
            m13 = @this.m13 + other.m13,
            m20 = @this.m20 + other.m20,
            m21 = @this.m21 + other.m21,
            m22 = @this.m22 + other.m22,
            m23 = @this.m23 + other.m23,
            m30 = @this.m30 + other.m30,
            m31 = @this.m31 + other.m31,
            m32 = @this.m32 + other.m32,
            m33 = @this.m33 + other.m33
        };

        /// <summary>
        /// Extract translation from transform matrix.
        /// </summary>
        /// <param name="thisMatrix">Transform matrix.</param>
        /// <returns>
        /// Translation offset.
        /// </returns>
        public static Vector3 ToTranslation(this Matrix4x4 thisMatrix) => new Vector3()
        {
            x = thisMatrix.m03,
            y = thisMatrix.m13,
            z = thisMatrix.m23
        };

        /// <summary>
        /// Extract rotation quaternion from transform matrix.
        /// </summary>
        /// <param name="this">Transform matrix.</param>
        /// <returns>
        /// Quaternion representation of rotation transform.
        /// </returns>
        public static Quaternion ToRotation(this Matrix4x4 @this)
        {
            var forward = new Vector3()
            {
                x = @this.m02,
                y = @this.m12,
                z = @this.m22
            };

            var upwards = new Vector3()
            {
                x = @this.m01,
                y = @this.m11,
                z = @this.m21
            };

            return Quaternion.LookRotation(forward, upwards);
        }

        /// <summary>
        /// Extract scale from transform matrix.
        /// </summary>
        /// <param name="this">Transform matrix.</param>
        /// <returns>
        /// Scale vector.
        /// </returns>
        public static Vector3 ToScale(this Matrix4x4 @this) => new Vector3()
        {
            x = new Vector4(@this.m00, @this.m10, @this.m20, @this.m30).magnitude,
            y = new Vector4(@this.m01, @this.m11, @this.m21, @this.m31).magnitude,
            z = new Vector4(@this.m02, @this.m12, @this.m22, @this.m32).magnitude
        };
    }
}