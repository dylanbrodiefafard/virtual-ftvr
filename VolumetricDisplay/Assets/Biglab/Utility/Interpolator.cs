using UnityEngine;

namespace Biglab.Utility
{
    /// <summary>
    /// Useful class for animating something over a fixed duration of real time.
    /// </summary>
    public sealed class Interpolator
        // Author: Christopher Chamberlain - 2017
    {
        /// <summary>
        /// Time this interpolator object was created.
        /// </summary>
        public readonly float StartTime;

        /// <summary>
        /// Length in seconds this interpolator computes a blend factor for.
        /// </summary>
        public readonly float Duration;

        /// <summary>
        /// Determines if this interpolator will return a factor larger than one.
        /// </summary>
        public readonly bool Clamped;

        /// <summary>
        /// Gets the current time since the creation of the object.
        /// </summary>
        public float Current => Mathf.Min(Time.time - StartTime, Duration);

        /// <summary>
        /// Gets the interpolation factor ( percent of duration ).
        /// </summary>
        public float Factor => Current / Duration;

        /// <summary>
        /// Gets the interpolation factor ( percent of duration ).
        /// </summary>
        public float DeltaFactor => Time.deltaTime / Duration;

        private Interpolator(float startTime, float duration, bool clamped)
        {
            StartTime = startTime;
            Duration = duration;
        }

        /// <summary>
        /// Creates a new interpolator.
        /// </summary>
        /// <param name="duration"> Length in seconds to interpolate over. </param>
        /// <param name="clamped"> Should it clamp to the elapsed time to the duration. </param>
        public static Interpolator CreateNew(float duration, bool clamped = true)
            => new Interpolator(Time.time, duration, clamped);

        /// <summary>
        /// Computes an interpolation factor that is independant of framerate.
        /// </summary>
        public static float ComputeFactor(float distance, float rate, float duration = 1F)
            => (rate / distance) * (Time.deltaTime * duration);

        #region Lerp

        /// <summary>
        /// Linear interpolation based on a distance over duration instead of percentage.
        /// </summary>
        public static Vector2 Lerp(Vector2 source, Vector2 target, float rate, float duration = 1F)
        {
            var d = Vector2.Distance(source, target);
            return Vector2.Lerp(source, target, ComputeFactor(d, rate));
        }

        /// <summary>
        /// Linear interpolation based on a distance over duration instead of percentage.
        /// </summary>
        public static Vector3 Lerp(Vector3 source, Vector3 target, float rate, float duration = 1F)
        {
            var d = Vector3.Distance(source, target);
            return Vector3.Lerp(source, target, ComputeFactor(d, rate));
        }

        /// <summary>
        /// Spherical interpolation based on a distance over duration instead of percentage.
        /// </summary>
        public static Vector3 Slerp(Vector3 source, Vector3 target, float rate, float duration = 1F)
        {
            var d = Vector3.Angle(source, target);
            return Vector3.Slerp(source, target, ComputeFactor(d, rate));
        }

        /// <summary>
        /// Spherical interpolation based on a distance over duration instead of percentage.
        /// </summary>
        public static Quaternion Slerp(Quaternion source, Quaternion target, float rate, float duration = 1F)
        {
            var d = Quaternion.Angle(source, target);
            return Quaternion.Slerp(source, target, ComputeFactor(d, rate));
        }

        #endregion
    }
}