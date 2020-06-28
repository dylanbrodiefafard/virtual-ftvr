using UnityEngine;

namespace Biglab.Utility.Transforms
{
    public class RotateAboutPosition : MonoBehaviour
    {
        [Tooltip("Leave this blank to rotate about this object.")]
        public Transform LocalSpace;

        [Tooltip("This position is defined in the LocalSpace transform coordinates.")]
        public Vector3 LocalPosition = Vector3.zero;

        [Tooltip("This axis is defined in the LocalSpace transform coordinates.")]
        public Vector3 LocalAxis = Vector3.up;

        [Tooltip("Make this negative to rotate the opposite way.")]
        public float RotationSpeed;

        #region monobehaviour

        void Start()
        {
            if (LocalSpace == null)
            {
                LocalSpace = transform;
            }
        }

        void Update()
        {
            transform.RotateAround(LocalSpace.TransformPoint(LocalPosition), LocalSpace.rotation * LocalAxis.normalized,
                RotationSpeed * Time.deltaTime);
        }

        #endregion
    }
}