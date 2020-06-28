using System;

namespace Biglab.Extensions
{
    public static class RandomExtensions
        // Author: Christopher Chamberlain - 2017
    {
        /// <summary>
        /// Generates a random number between 0.0 and 1.0.
        /// </summary>
        public static float NextFloat(this Random @this)
        {
            return (float)@this.NextDouble();
        }

        /// <summary>
        /// Generates a random number between the <param name="min">minimum</param> and <param name="max">maximum</param> range.
        /// </summary>
        public static float NextFloat(this Random @this, float min, float max)
        {
            return @this.NextFloat() * (max - min) + min;
        }

        /// <summary>
        /// Generates a random number between the <param name="min">minimum</param> and <param name="max">maximum</param> range.
        /// </summary>
        public static double NextDouble(this Random @this, double min, double max)
        {
            return @this.NextDouble() * (max - min) + min;
        }
    }
}