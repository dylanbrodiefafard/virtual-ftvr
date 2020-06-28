using UnityEngine;

namespace Biglab.Utility
{
    /// <summary>
    /// An component to trigger collision like events for a raycast.
    /// </summary>
    public class Raycaster : MonoBehaviour
    {
        private const string onExitMethod = "OnRaycastExit";
        private const string onEnterMethod = "OnRaycastEnter";
        private const string onStayMethod = "OnRaycastStay";

        public LayerMask LayerMask = -1;

        public float MaxDistance = float.MaxValue;

        [Space]

        [ReadOnly, SerializeField]
        private Collider _prevCollider;

        private RaycastHit _raycastHit;

        public RaycastHit GetRaycastHit()
            => _raycastHit;

        void FixedUpdate()
        {
            var ray = new Ray(transform.position, transform.forward);

            // 
            if (Physics.Raycast(ray, out _raycastHit, MaxDistance, LayerMask))
            {
                var collider = _raycastHit.collider;

                // Collider wasn't the same one as last check
                if (collider != _prevCollider)
                {
                    // Our last collider wasn't null, so exit that collider
                    if (_prevCollider != null)
                    {
                        _prevCollider.SendMessage(onExitMethod, this, SendMessageOptions.DontRequireReceiver);
                    }

                    // Inform the new collider we've entered it
                    collider.SendMessage(onEnterMethod, this, SendMessageOptions.DontRequireReceiver);
                }
                else
                {
                    // We've persisted in the same object, send a new update
                    collider.SendMessage(onStayMethod, this, SendMessageOptions.DontRequireReceiver);
                }

                // Set last collider
                _prevCollider = collider;
            }
            else
            {
                // 
                _raycastHit = default(RaycastHit);

                // We did not raycast anything, and it wasn't null
                // send exit event and set to null.
                if (_prevCollider != null)
                {
                    _prevCollider.SendMessage(onExitMethod, this, SendMessageOptions.DontRequireReceiver);
                    _prevCollider = null;
                }
            }
        }
    }
}