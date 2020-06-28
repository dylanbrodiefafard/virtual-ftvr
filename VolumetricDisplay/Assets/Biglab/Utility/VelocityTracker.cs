using UnityEngine;

namespace Biglab.Utility
{
    public class VelocityTracker : MonoBehaviour
    {
        public float AngularVelocity => _angularVelocity;

        public Vector3 Acceleration => _acceleration;

        public Vector3 Velocity => _velocity;

        public float Speed => _speed;

        public float SmoothingFactor
        {
            get { return _smoothingFactor; }
            set { _smoothingFactor = value; }
        }

        [SerializeField] [Range(1F, 100F)] private float _smoothingFactor = 2F;

        [Space] [ReadOnly, SerializeField] private Vector3 _acceleration;

        [Space] [ReadOnly, SerializeField] private Vector3 _velocity;

        [ReadOnly, SerializeField] private float _speed;

        [Space] [ReadOnly, SerializeField] private float _angularVelocity;

        private Vector3 _previousPosition;
        private Vector3 _previousVelocity;
        private Quaternion _previousRotation;

        #region MonoBehaviour

        void Start()
        {
            _previousPosition = transform.position;
            _previousRotation = transform.rotation;
            _previousVelocity = Vector3.zero;
        }

        private void Update()
        {
            var currentPosition = transform.position;
            var currentRotation = transform.rotation;
            var currentFrameVelocity = (currentPosition - _previousPosition) / Time.deltaTime;
            var currentFrameAngularVelocity = Quaternion.Angle(currentRotation, _previousRotation) / Time.deltaTime;
            var currentFrameAcceleration = (currentFrameVelocity - _previousVelocity) / Time.deltaTime;
            var currentFrameSpeed = currentFrameVelocity.magnitude;

            // Update with approximation of exponential moving average
            _acceleration -= _acceleration / _smoothingFactor;
            _acceleration += currentFrameAcceleration / _smoothingFactor;
            _velocity -= _velocity / _smoothingFactor;
            _velocity += currentFrameVelocity / _smoothingFactor;
            _speed -= _speed / _smoothingFactor;
            _speed += currentFrameSpeed / _smoothingFactor;
            _angularVelocity -= _angularVelocity / _smoothingFactor;
            _angularVelocity += currentFrameAngularVelocity / _smoothingFactor;

            // 
            _previousPosition = currentPosition;
            _previousRotation = currentRotation;
            _previousVelocity = currentFrameVelocity;
        }

        #endregion
    }
}