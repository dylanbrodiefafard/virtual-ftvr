using UnityEngine;

namespace Biglab.Utility.Transforms
{
    /// <summary>
    /// Orients an object to be a 'billboard' by rotating it to align with the camera forward vector.
    /// </summary>
    public class OrientBillboard : MonoBehaviour
        // Author: Christopher Chamberlain - 2017
    {
        /// <summary>
        /// Negates the forward vector to flip which side of the object looks at the camera.
        /// </summary>
        [Tooltip("Negates the forward vector to flip which side of the object looks at the camera.")]
        public bool FlipFront;

        private void OnWillRenderObject()
        {
            var dir = Camera.current.transform.forward;
            if (FlipFront)
            {
                dir = -dir;
            }

            // Compute and assign rotation
            transform.rotation = Quaternion.LookRotation(dir);
        }
    }
}