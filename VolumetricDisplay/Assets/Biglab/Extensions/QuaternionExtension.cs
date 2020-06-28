using UnityEngine;

namespace Biglab.Extensions
{
    public static class QuaternionExtension
    {
        /// <summary>
        /// Outer product of two Quaternions represented as Vectors.
        /// </summary>
        /// <param name="this">The first quaternion.</param>
        /// <param name="other">The second quaternion.</param>
        /// <returns>The outer product matrix of the two quaternions.</returns>
        public static Matrix4x4 OuterProduct(this Quaternion @this, Quaternion other)
        {
            return new Matrix4x4
            {
                // Row 1
                m00 = @this.w * other.w, // Column 1
                m01 = @this.w * other.x, // Column 2
                m02 = @this.w * other.y, // Column 3
                m03 = @this.w * other.z, // Column 4

                // Row 2
                m10 = @this.x * other.w, // Column 1
                m11 = @this.x * other.x, // Column 2
                m12 = @this.x * other.y, // Column 3
                m13 = @this.x * other.z, // Column 4

                // Row 3
                m20 = @this.y * other.w, // Column 1
                m21 = @this.y * other.x, // Column 2
                m22 = @this.y * other.y, // Column 3
                m23 = @this.y * other.z, // Column 4

                // Row 4
                m30 = @this.z * other.w, // Column 1
                m31 = @this.z * other.x, // Column 2
                m32 = @this.z * other.y, // Column 3
                m33 = @this.z * other.z, // Column 4
            };
        }
    }
}