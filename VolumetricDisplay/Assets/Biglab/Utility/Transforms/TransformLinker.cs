using UnityEngine;

namespace Biglab.Utility.Transforms
{
    public class TransformLinker : MonoBehaviour
        // TODO: CC: Why TargetGameObject and _targetTransform?
    {
        public GameObject TargetGameObject;
        public GameObject SourceGameObject;

        public bool IsSourceTransformLocal;
        public bool IsTargetTransformLocal;

        public bool ShouldLinkRotation = true;
        public bool ShouldLinkPosition = true;

        public bool IsSourceThisTransform = true;
        public bool IsTargetThisTransform;

        private Transform _sourceTransform;
        private Transform _targetTransform;

        public void UpdateTarget(Transform pTransform)
            => _targetTransform = pTransform;

        public void UpdateSource(Transform pTransform)
            => _sourceTransform = pTransform;

        #region MonoBehaviour

        private void Update()
        {
            if (IsSourceThisTransform)
            {
                _sourceTransform = transform;
            }

            if (IsTargetThisTransform)
            {
                _targetTransform = transform;
            }

            // Exit, we don't have a proper source and target
            if (!_targetTransform)
            {
                return;
            }

            if (!_sourceTransform)
            {
                return;
            }

            if (ShouldLinkPosition)
            {
                var targetPosition =
                    IsSourceTransformLocal ? _sourceTransform.localPosition : _sourceTransform.position;

                if (IsTargetTransformLocal)
                {
                    _targetTransform.localPosition = targetPosition;
                }
                else
                {
                    _targetTransform.position = targetPosition;
                }
            }

            if (!ShouldLinkRotation)
            {
                return;
            }

            var targetRotation = IsSourceTransformLocal ? _sourceTransform.localRotation : _sourceTransform.rotation;

            if (IsTargetTransformLocal)
            {
                _targetTransform.localRotation = targetRotation;
            }
            else
            {
                _targetTransform.rotation = targetRotation;
            }
        }

        private void Start()
        {
            if (IsTargetThisTransform)
            {
                TargetGameObject = gameObject;
            }

            if (IsSourceThisTransform)
            {
                SourceGameObject = gameObject;
            }

            if (TargetGameObject)
            {
                UpdateTarget(TargetGameObject.transform);
            }

            if (SourceGameObject)
            {
                UpdateSource(SourceGameObject.transform);
            }
        }

        #endregion
    }
}