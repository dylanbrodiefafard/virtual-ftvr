using UnityEngine;

namespace Biglab.Extensions
{
    public static class VectorUtil
    // Author: Christopher Chamberlain - 2017
    {
        #region Vector2 Extensions

        /// <summary>
        /// Multiplies vector A with B ( per component ).
        /// </summary>
        public static Vector2 Multiply(this Vector2 a, Vector2 b)
            => new Vector2
            {
                x = a.x * b.x,
                y = a.y * b.y
            };

        /// <summary>
        /// Divides vector A with B ( per component ).
        /// </summary>
        public static Vector2 Divide(this Vector2 a, Vector2 b)
            => new Vector2
            {
                x = a.x / b.x,
                y = a.y / b.y
            };

        public static float MaxElement(this Vector2 @this)
            => Mathf.Max(@this.x, @this.y);

        public static float MinElement(this Vector2 @this)
            => Mathf.Min(@this.x, @this.y);

        #endregion

        #region Vector3 Extensions

        /// <summary>
        /// Multiplies vector A with B ( per component ).
        /// </summary>
        public static Vector3 Multiply(this Vector3 a, Vector3 b)
            => new Vector3
            {
                x = a.x * b.x,
                y = a.y * b.y,
                z = a.z * b.z,
            };

        /// <summary>
        /// Divides vector A with B ( per component ).
        /// </summary>
        public static Vector3 Divide(this Vector3 a, Vector3 b)
            => new Vector3
            {
                x = a.x / b.x,
                y = a.y / b.y,
                z = a.z / b.z,
            };

        /// <summary>
        /// Inverses each component of the vector.
        /// </summary>
        /// <param name="this">The vector to invert elements of.</param>
        /// <returns>A vector3 where each element is inverted.</returns>
        public static Vector3 Inverse(this Vector3 @this)
            => new Vector3
            {
                x = 1 / @this.x,
                y = 1 / @this.y,
                z = 1 / @this.z
            };

        public static float MaxElement(this Vector3 @this)
            => Mathf.Max(@this.x, @this.y, @this.z);

        public static float MinElement(this Vector3 @this)
            => Mathf.Min(@this.x, @this.y, @this.z);

        #endregion

        #region Vector4 Extensions

        /// <summary>
        /// Multiplies vector A with B ( per component ).
        /// </summary>
        public static Vector4 Multiply(this Vector4 a, Vector4 b)
            => new Vector4
            {
                x = a.x * b.x,
                y = a.y * b.y,
                z = a.z * b.z,
                w = a.w * b.w,
            };

        /// <summary>
        /// Divides vector A with B ( per component ).
        /// </summary>
        public static Vector4 Divide(this Vector4 a, Vector4 b)
            => new Vector4
            {
                x = a.x / b.x,
                y = a.y / b.y,
                z = a.z / b.z,
                w = a.w / b.w,
            };

        public static float MaxElement(this Vector4 @this)
            => Mathf.Max(@this.x, @this.y, @this.z, @this.w);

        public static float MinElement(this Vector4 @this)
            => Mathf.Min(@this.x, @this.y, @this.z, @this.w);

        #endregion
    }
}