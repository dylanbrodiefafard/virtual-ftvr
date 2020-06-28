using UnityEngine;

namespace Biglab.Math
{
    /// <summary>
    /// This class contains useful extensions to Unity's Random class <see cref="Random"/>.
    /// Calling <see cref="Random.InitState(int)"/> will affect the state of these calls. 
    /// </summary>
    public static class RandomUtilities
    {
        /// <summary>
        /// Sets the random seed for any of the functions in this class.
        /// </summary>
        /// <param name="seed">The seed to be used to set the state of the random engine.</param>
        public static void SetSeed(int seed)
            => Random.InitState(seed);

        /// <summary>
        /// Simulates the probability a fair coin toss landing heads up.
        /// </summary>
        public static bool IsHeads
            => Random.value < 0.5f;

        /// <summary>
        /// Simulates the probability a fair coin toss landing tails up.
        /// </summary>
        public static bool IsTails
            => Random.value > 0.5f;

        /// <summary>
        /// Get a random number from a normal distribution with given mean and standard deviation.
        /// </summary>
        /// <description>
        /// Get a random number with probability following a normal distribution with given mean
        /// and standard deviation. The likelihood of getting any given number corresponds to
        /// its value along the y-axis in the distribution described by the parameters.
        /// </description>
        /// <returns>
        /// A random number between -infinity and infinity, with probability described by the distribution.
        /// </returns>
        /// <param name="mean">The Mean (or center) of the normal distribution.</param>
        /// <param name="stdDev">The Standard Deviation (or Sigma) of the normal distribution.</param>
        public static float RandomNormalDistribution(float mean, float stdDev)
            => RandomFromStandardNormalDistribution() * stdDev + mean;

        /// <summary>
        /// Get a random number from the standard normal distribution.
        /// </summary>
        /// <returns>
        /// A random number in range [-inf, inf] from the standard normal distribution (mean == 1, stand deviation == 1).
        /// </returns>
        public static float RandomFromStandardNormalDistribution()
        {
            // This code follows the polar form of the muller transform:
            // https://en.wikipedia.org/wiki/Box%E2%80%93Muller_transform#Polar_form
            // also known as Marsaglia polar method 
            // https://en.wikipedia.org/wiki/Marsaglia_polar_method

            // calculate points on a circle
            float u, v;

            float s; // this is the hypotenuse squared.
            do
            {
                u = Random.Range(-1f, 1f);
                v = Random.Range(-1f, 1f);
                s = (u * u) + (v * v);
            } while (Mathf.Approximately(s, 0) || s >= 1); // keep going until s is nonzero and less than one

            // choose between u and v for seed (z0 vs z1)
            var seed = IsTails ? u : v;

            // return normally distributed number.
            return seed * Mathf.Sqrt(-2.0f * Mathf.Log(s) / s);
        }

        /// <summary>
        /// Computes the probability of K events happening in time interval T with a rate (1 / time) of rate.
        /// </summary>
        /// <param name="k">the number of events to occur</param>
        /// <param name="t">the interval of time over which the events can occur</param>
        /// <param name="rate">the rate at which events occur</param>
        /// <returns>The probability of k events in interval t with rate rate</returns>
        public static float ProbabilityKEventsInIntervalT(int k, float t, float rate) 
            => Mathf.Exp(-rate * t) * (Mathf.Pow(rate * t, k) / MathB.Factorial(k));


        public static Quaternion RandomClampedRotation(float maxDegrees)
            => Quaternion.AngleAxis(Random.value * maxDegrees, Random.rotation * Vector3.up);
    }
}