using UnityEngine;

namespace Biglab.Utility.Transforms
{
    /// <inheritdoc />
    /// <summary>
    /// Much like <see cref="T:Biglab.Utility.Transforms.LookAtTarget" /> but also positions the object on a spherical shell.
    /// </summary>
    public class OrientTangent : MonoBehaviour
        // TODO: CC: What is the OnSphere behaviour? I don't understand its purpose.
    {
        [Tooltip("The transform that this will look at along the viewport spherical tangent.")]
        public Transform Target;

        [Tooltip("The local transfopm ( ie, viewport, spheree ) transform that this will look at from.")]
        public Transform Local;

        [Tooltip("The radius of the local sphere that this object will be positioned on.")]
        public float Radius = 1F;

        /// <summary>
        /// Negates the forward vector to flip which side of the object looks at the target.
        /// </summary>
        [Tooltip("Negates the forward vector to flip which face of the object looks at the target.")]
        public bool FlipFront;

        /// <summary>
        /// Negates the forward vector to flip which side of the object looks at the target.
        /// </summary>
        [Tooltip("Negates the forward vector to flip which side of the object looks at the target.")]
        public bool FlipSide;

        public bool OnSphere;

        private void LateUpdate()
        {
            if (OnSphere)
            {
                //  
                var dir = Vector3.Normalize(Target.position - Local.position);

                // Place tangent plane at radius
                transform.position = Local.position + ((FlipSide ? dir : -dir) * Local.lossyScale.x * 0.5F);

                // 
                transform.rotation = Quaternion.LookRotation(FlipFront ? -dir : dir);
            }
            else
            {
                var mainCamera = GameObject.Find("Main Camera");
                var mainCameraTransform = mainCamera.transform;
                var mainCameraCamera = mainCamera.GetComponent<Camera>();
                transform.position = mainCameraTransform.position +
                                     mainCameraCamera.cameraToWorldMatrix.MultiplyVector(new Vector3(0, 0,
                                         -mainCameraCamera.nearClipPlane));
            }
        }
    }
}