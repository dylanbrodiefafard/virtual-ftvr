using System.Collections;
using Biglab.Extensions;
using UnityEngine;

namespace Biglab.Utility.Controllers
{
    public class SingleOculusTouchManipulator : MonoBehaviour
    {
        private static bool IsButtonOneDown => OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.Touch);
        private static bool IsButtonTwoDown => OVRInput.Get(OVRInput.Button.Two, OVRInput.Controller.Touch);
        private bool IsTranslationActive => IsButtonOneDown && Time.time - _lastButtonOneDownTime > _secondToActivate;
        private bool IsRotationActive => IsButtonTwoDown && Time.time - _lastButtonTwoDownTime > _secondToActivate;

        public Transform Hand;

        public float RotationInterpolationSpeed = 20.0f;
        public float TranslationInterpolationSpeed = 20.0f;

        private Vector3 _originalPosition;
        private Quaternion _originalRotation;

        private Vector3 _previousPosition;
        private Quaternion _previousRotation;

        private Quaternion _targetRotation;
        private Vector3 _targetPosition;

        private bool _isResetting;

        private float _lastButtonOneDownTime;
        private float _lastButtonTwoDownTime;
        private bool _wasButtonOneDownLastFrame;
        private bool _wasButtonTwoDownLastFrame;

        private const float _secondToActivate = 0.125f;

        private void Start()
        {
            _originalRotation = _targetRotation = transform.rotation;
            _originalPosition = _targetPosition = transform.position;
            _previousPosition = Hand.position;
            _previousRotation = Hand.rotation;
        }

        private void Update()
        {
            if (IsButtonOneDown && !_wasButtonOneDownLastFrame)
            {
                _wasButtonOneDownLastFrame = true;
                _lastButtonOneDownTime = Time.time;
            }
            else if (!IsButtonOneDown)
            {
                _wasButtonOneDownLastFrame = false;
            }

            if (IsButtonTwoDown && !_wasButtonTwoDownLastFrame)
            {
                _wasButtonTwoDownLastFrame = true;
                _lastButtonTwoDownTime = Time.time;
            }
            else if (!IsButtonTwoDown)
            {
                _wasButtonTwoDownLastFrame = false;
            }

            if (!_isResetting && IsRotationActive)
            {
                UpdateTargetRotation();
            }

            if (!_isResetting && IsTranslationActive)
            {
                UpdateTargetPosition();
            }

            transform.position = Vector3.Lerp(transform.position, _targetPosition,
                Time.deltaTime * TranslationInterpolationSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation,
                Time.deltaTime * RotationInterpolationSpeed);

            _previousPosition = Hand.position;
            _previousRotation = Hand.rotation;
        }

        private void UpdateTargetRotation()
        {
            var deltaRotation = Hand.rotation * Quaternion.Inverse(_previousRotation);

            _targetRotation = deltaRotation * _targetRotation;
        }

        private void UpdateTargetPosition()
        {
            var deltaPosition = Hand.position - _previousPosition;

            _targetPosition += deltaPosition;
        }

        public IEnumerator ResetOrientation(float secondsToTake)
        {
            _isResetting = true;

            var startTime = Time.time;
            var endTime = startTime + secondsToTake;
            var startPosition = transform.position;
            var startRotation = transform.rotation;

            do
            {
                var t = Time.time.Rescale(startTime, endTime, 0, 1);
                _targetPosition = Vector3.Lerp(startPosition, _originalPosition, t);
                _targetRotation = Quaternion.Slerp(startRotation, _originalRotation, t);

                yield return new WaitForEndOfFrame();
            } while (Time.time < endTime);

            _isResetting = false;
        }
    }
}