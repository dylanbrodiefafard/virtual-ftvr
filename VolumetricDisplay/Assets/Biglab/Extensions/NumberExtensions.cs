using UnityEngine;

namespace Biglab.Extensions
{
    public static class NumberExtensions
    {
        /// <summary>
        /// Remaps this numbers min max domain to another.
        /// </summary>
        /// <param name="this"></param>
        /// <param name="fromMin"></param>
        /// <param name="fromMax"></param>
        /// <param name="toMin"></param>
        /// <param name="toMax"></param>
        /// <returns></returns>
        public static float Rescale(this float @this, float fromMin, float fromMax, float toMin, float toMax)
        {
            return Mathf.Lerp(toMin, toMax, @this.Between(fromMin, fromMax));
            // return toMin + (toMax - toMin) * ((fromMin - @this) / (fromMin - fromMax));
        }

        /// <summary>
        /// Gets the blending value ( 0 to 1 ) of this number between the min and max.
        /// </summary>
        public static float Between(this float @this, float min, float max)
        {
            return (min - @this) / (min - max);
        }

        /// <summary>
        /// Remaps this numbers min max domain to another.
        /// </summary>
        /// <param name="this"></param>
        /// <param name="fromMin"></param>
        /// <param name="fromMax"></param>
        /// <param name="toMin"></param>
        /// <param name="toMax"></param>
        /// <returns></returns>
        public static float Rescale(this int @this, float fromMin, float fromMax, float toMin, float toMax)
        {
            return Mathf.LerpUnclamped(toMin, toMax, @this.Between(fromMin, fromMax));
            // return toMin + (toMax - toMin) * ((fromMin - @this) / (fromMin - fromMax));
        }

        /// <summary>
        /// Gets the blending value ( 0 to 1 ) of this number between the min and max.
        /// </summary>
        public static float Between(this int @this, float min, float max)
        {
            return (min - @this) / (min - max);
        }

        // ******************************************************************
        // Base on Hans Passant Answer on:
        // https://stackoverflow.com/questions/2411392/double-epsilon-for-equality-greater-than-less-than-less-than-or-equal-to-gre

        /// <summary>
        /// Compare two double taking in account the double precision potential error.
        /// Take care: truncation errors accumulate on calculation. More you do, more you should increase the epsilon.
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <param name="precalculatedContextualEpsilon"></param>
        /// <returns></returns>
        public static bool AboutEquals(this double value1, double value2)
        {
            var epsilon = System.Math.Max(System.Math.Abs(value1), System.Math.Abs(value2)) * 1E-15;
            return System.Math.Abs(value1 - value2) <= epsilon;
        }

        // ******************************************************************
        // Base on Hans Passant Answer on:
        // https://stackoverflow.com/questions/2411392/double-epsilon-for-equality-greater-than-less-than-less-than-or-equal-to-gre

        /// <summary>
        /// Compare two double taking in account the double precision potential error.
        /// Take care: truncation errors accumulate on calculation. More you do, more you should increase the epsilon.
        /// You get really better performance when you can determine the contextual epsilon first.
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <param name="precalculatedContextualEpsilon"></param>
        /// <returns></returns>
        public static bool AboutEquals(this double value1, double value2, double precalculatedContextualEpsilon) 
            => System.Math.Abs(value1 - value2) <= precalculatedContextualEpsilon;

        // ******************************************************************
        public static double GetContextualEpsilon(this double biggestPossibleContextualValue) 
            => biggestPossibleContextualValue * 1E-15;

        // ******************************************************************
        /// <summary>
        /// Mathlab equivalent
        /// </summary>
        /// <param name="dividend"></param>
        /// <param name="divisor"></param>
        /// <returns></returns>
        public static double Mod(this double dividend, double divisor) 
            => dividend - System.Math.Floor(dividend / divisor) * divisor;
    }
}