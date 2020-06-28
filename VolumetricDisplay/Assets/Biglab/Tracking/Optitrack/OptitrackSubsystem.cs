using System.Collections.Generic;
using UnityEngine;

namespace Biglab.Tracking.Optitrack
{
    public class OptitrackSubsystem : TrackingSubsystem
    {
        public OptitrackStreamingClient StreamingClient;
        public override Transform TrackingSpace { get; protected set; }

        private Dictionary<int, Transform> _anchors;

        private void Awake()
        {
            _anchors = new Dictionary<int, Transform>();

            StreamingClient = gameObject.AddComponent<OptitrackStreamingClient>();
        }

        private void Start()
            => IsReady = true;

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
            var go = new GameObject($"Optitrack Object: {id}");
            // go.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor | HideFlags.NotEditable;
            go.transform.SetParent(transform);

            var optitrackRigidBody = go.AddComponent<OptitrackRigidBody>();
            optitrackRigidBody.StreamingClient = StreamingClient;
            optitrackRigidBody.RigidBodyId = id;

            return go.transform;
        }
    }
}