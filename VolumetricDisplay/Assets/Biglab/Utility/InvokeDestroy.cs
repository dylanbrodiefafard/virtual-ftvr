using UnityEngine;

namespace Biglab.Utility
{
    /// <summary>
    /// Component to automatically destroy the object after the given time.
    /// This is useful for things like audio sources or temporary particle emitters.
    /// </summary>
    public class InvokeDestroy : MonoBehaviour
        // Author: Christopher Chamberlain - 2017
    {
        /// <summary>
        /// Time in seconds before an automatic call to destroy the game object.
        /// </summary>
        [Tooltip("Time in seconds before an automatic call to destroy the game object.")]
        public float Time = 2;

        private void Start()
        {
            Invoke(nameof(Kill), Time);
        }

        private void Kill()
        {
            Destroy(gameObject);
        }
    }
}