using UnityEngine;
using UnityEngine.Serialization;

namespace Biglab.Utility.Transforms
{
    /// <summary>
    /// A component to animate the object to look at the target object.
    /// </summary>
    public class LookAtTarget : MonoBehaviour
    {
        [Tooltip("The desired target to look at.")]
        public Transform Target;

        [FormerlySerializedAs("IsSlerped")]
        [Tooltip("Should this object animate towards the target via interpolation?")]
        public bool IsInterpolated = true;

        [FormerlySerializedAs("SlerpSpeed")]
        [Tooltip("If interpolated, how quickly should this animate towards the target.")]
        public float InterpolationSpeed = 1.0f;

        [FormerlySerializedAs("LookUpDirection")] [Tooltip("Which up to use?")]
        public UpDirection Up = UpDirection.World;

        [Tooltip("Negates the direction vector to flip which side of the object is facing the target.")]
        public bool FlipFront;

        public enum UpDirection
        {
            World,
            Local,
            Target
        }

        #region MonoBehaviour

        private void Update()
        {
            if (!Target)
            {
                return;
            }

            var forwardDirection = GetForwardDirection();
            var upDirection = GetUpDirection();

            if (Mathf.Approximately(forwardDirection.magnitude, 0))
                return;

            if (Mathf.Approximately(upDirection.magnitude, 0))
                return;

            var targetRotation = Quaternion.LookRotation(forwardDirection, upDirection);
            transform.rotation = IsInterpolated
                ? Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * InterpolationSpeed)
                : targetRotation;
        }

        #endregion

        private Vector3 GetForwardDirection()
        {
            var dir = (Target.position - transform.position).normalized;
            if (FlipFront)
            {
                dir = -dir;
            }

            return dir;
        }

        private Vector3 GetUpDirection()
        {
            switch (Up)
            {
                case UpDirection.World:
                    return Vector3.up;

                case UpDirection.Local:
                    return transform.up;

                case UpDirection.Target:
                    return Target.transform.up;

                default:
                    return Vector3.up;
            }
        }
    }
}