using Biglab.Extensions;
using Biglab.Math;
using UnityEngine;
using UnityInput = UnityEngine.Input;

namespace Biglab.Utility.Controllers
{
    public class OrbitFocusWithLook : MonoBehaviour
    {
        public Transform Focus;
        public float MinimumZoomDistance = 1.0f;
        public float MaximumZoomDistance = 2.0f;
        public int LookMouseButton;
        public int OribtMouseButton = 1;
        public float SlerpSpeed = 1.0f;
        public float LerpSpeed = 1.0f;
        public float OrbitSensitivity = 1.0f;
        public float LookSensitivity = 1.0f;
        public float ZoomSensitivity = 1.0f;
        public bool IsLookEnabled;

        private Vector3 _previousMousePosition;
        private bool _hasLookedAfterOrbit;
        private Vector3 _targetSphericalPosition;
        public const float PixelToRadian = 0.003f;

        protected Vector3 GetTargetPosition(bool pIsOrbiting, Vector3 deltaMousePosition, float zoomAxis)
        {
            if (pIsOrbiting)
            {
                var deltaTheta = deltaMousePosition.x * PixelToRadian * OrbitSensitivity;
                var deltaPhi = deltaMousePosition.y * PixelToRadian * OrbitSensitivity;
                _targetSphericalPosition +=
                    new Vector3(0, deltaTheta * Mathf.Sin(_targetSphericalPosition.z), deltaPhi);
                _targetSphericalPosition.y = _targetSphericalPosition.y % (2 * Mathf.PI);
                _targetSphericalPosition.z = _targetSphericalPosition.z % (2 * Mathf.PI);
            }

            var deltaR = zoomAxis * ZoomSensitivity;

            _targetSphericalPosition.x = Mathf.Clamp(_targetSphericalPosition.x + deltaR, MinimumZoomDistance,
                MaximumZoomDistance);
            return Focus.TransformPoint(MathB.SphericalToCartesian(_targetSphericalPosition));
        }

        protected Quaternion GetTargetRotation(bool pIsOrbiting, bool hasStartedLooking, Vector3 deltaMousePosition)
        {
            if (!IsLookEnabled || pIsOrbiting || !hasStartedLooking)
            {
                return Quaternion.LookRotation(Focus.transform.position - transform.position, Focus.up);
            }

            var deltaEuler = new Vector3(-deltaMousePosition.y, deltaMousePosition.x, 0) * LookSensitivity;
            var currentEuler = transform.rotation.eulerAngles;
            currentEuler.z = 0;
            return Quaternion.Euler(currentEuler + deltaEuler);
        }

        #region MonoBehaviour

        private void Start()
        {
            if (Focus.IsNull())
            {
                return;
            }

            _targetSphericalPosition = MathB.CartesianToSpherical(transform.position);
            transform.position = GetTargetPosition(false, Vector3.zero, 0);
            transform.rotation = GetTargetRotation(false, false, Vector3.zero);
        }

        private void Update()
        {
            if (Focus.IsNull())
            {
                return;
            }

            var deltaMousePosition = Vector3.zero;
            var currentMousePosition = UnityInput.mousePosition;
            var isOrbitButtonDown = UnityInput.GetMouseButton(OribtMouseButton);
            var isLookButtonDown = UnityInput.GetMouseButton(LookMouseButton);

            if (UnityInput.GetMouseButtonDown(OribtMouseButton) || UnityInput.GetMouseButtonDown(LookMouseButton))
            {
                _previousMousePosition = currentMousePosition;
            }

            if (isOrbitButtonDown)
            {
                _hasLookedAfterOrbit = false;
            }
            else if (isLookButtonDown)
            {
                _hasLookedAfterOrbit = true;
            }

            if (isOrbitButtonDown || isLookButtonDown)
            {
                deltaMousePosition = currentMousePosition - _previousMousePosition;
                _previousMousePosition = currentMousePosition;
            }

            var targetPosition = GetTargetPosition(isOrbitButtonDown, deltaMousePosition,
                -UnityInput.GetAxis("Mouse ScrollWheel"));
            var targetRotation = GetTargetRotation(isOrbitButtonDown, _hasLookedAfterOrbit, deltaMousePosition);

            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * LerpSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * SlerpSpeed);
        }

        #endregion MonoBehaviour
    }
}