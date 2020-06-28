using System.Collections.Generic;
using UnityEngine;

namespace Biglab.Tracking.Fixed
{
    public class FixedSubsystem : TrackingSubsystem
    {
        public override Transform TrackingSpace { get; protected set; }

        private Dictionary<int, Transform> _anchors;

        private void Awake()
        {
            _anchors = new Dictionary<int, Transform>();
        }

        private void Start()
        {
            IsReady = true;
        }

        public override Transform GetTrackingAnchor(int id)
        {
            // Return existing
            if (_anchors.ContainsKey(id))
            {
                return _anchors[id];
            }

            // Createa
            var anchor = CreateTrackingAnchor(id);
            _anchors[id] = anchor;
            return anchor;
        }

        private Transform CreateTrackingAnchor(int id)
        {
            var go = new GameObject($"Fixed Tracking Object: {id}");
            go.transform.SetParent(transform);

            go.transform.position = new Vector3(1, 1 / 2F, 1);

            return go.transform;
        }
    }
}