using System.Diagnostics;

namespace Biglab.Utility
{
    /// <summary>
    /// A utility object for performing rate limited actions.
    /// </summary>
    public sealed class RateLimiter
    {
        private long _then;

        /// <summary>
        /// Creates a new rate limiter.
        /// </summary>
        /// <param name="duration"> Time in seconds for when <see cref="CheckElapsedTime"/> will return true </param>
        public RateLimiter(float duration)
        {
            _then = Stopwatch.GetTimestamp();
            Duration = duration;
        }

        /// <summary>
        /// Time in seconds for when <see cref="CheckElapsedTime"/> will return true. <para/>
        /// Will set to zero if given a NaN or negative number.
        /// </summary>
        public float Duration
        {
            get { return _duration; }

            set
            {
                // Prevent NaN and only accept positive numbers.
                if (float.IsNaN(value)) { value = 0; }
                if (value < 0) { value = 0; }

                _duration = value;
            }
        }

        private float _duration;

        /// <summary>
        /// Checks if the amount of elapsed time exceeds the duration period.
        /// If the elapsed time exceeded the duration period, this will returns true and reset the elapsed time otherwise returns false.
        /// </summary>
        public bool CheckElapsedTime()
        {
            // Elapsed time since last tick ( in seconds )
            var elapsed = (Stopwatch.GetTimestamp() - _then) / (double)Stopwatch.Frequency;

            // If enough time has passed
            if (elapsed > Duration)
            {
                _then = Stopwatch.GetTimestamp();
                return true;
            }

            // Not enough time has pased.
            return false;
        }
    }
}