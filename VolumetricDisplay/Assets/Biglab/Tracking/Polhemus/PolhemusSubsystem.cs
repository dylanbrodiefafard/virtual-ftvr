using Biglab.Extensions;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Biglab.Tracking.Polhemus
{
    public class PolhemusSubsystem : TrackingSubsystem
    {
        public PlStream Stream;
        public override Transform TrackingSpace { get; protected set; }

        private Dictionary<int, Transform> _anchors;

        private void Awake()
        {
            Stream = gameObject.AddComponentWithInit<PlStream>(stream =>
            {
                stream.tracker_type = Config.Polhemus.GetTrackerType();
                stream.max_systems = Config.Polhemus.MaxSystems;
                stream.max_sensors = Config.Polhemus.MaxSensors;
            });
            _anchors = new Dictionary<int, Transform>();

            IsReady = true;
        }

        private void Update()
        {
            // Toggle handedness change boolean
            if (Input.GetKeyDown(KeyCode.F1))
            {
                Config.Polhemus.ChangeHandedness = !Config.Polhemus.ChangeHandedness;
            }
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
            var go = new GameObject($"Polhemus Object: {id}")
            {
                hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor | HideFlags.NotEditable
            };
            go.transform.SetParent(transform);

            go.AddComponentWithInit<PolhemusRigidBody>(rb =>
            {
                rb.TrackingSubsystem = this;
                rb.RigidBodyId = id;
            });

            return go.transform;
        }

        /// <summary>
        /// Gets the position corresponding to the given rigid body ID.
        /// </summary>
        /// <param name="rigidBodyId">index of the rigid body.</param>
        /// <returns>The position of the rigid body with handedness correction applied.</returns>
        public Vector3 GetPosition(int rigidBodyId)
        {
            if (rigidBodyId < 0 || rigidBodyId >= Stream.positions.Length)
            {
                throw new ArgumentOutOfRangeException(
                    $"{nameof(rigidBodyId)} is out of range of the Stream position array.");
            }

            var position = Stream.positions[rigidBodyId];
            var flipCorrection = Config.Polhemus.ChangeHandedness ? -1 : 1;

            // doing crude (90 degree) rotations into frame - Polhemus
            // Applying flip correction here
            return new Vector3
            {
                x = position.y * flipCorrection,
                y = -position.z,
                z = position.x
            };
        }

        /// <summary>
        /// Gets the rotation corresponding to the given rigid body ID.
        /// </summary>
        /// <param name="rigidBodyId">index of the rigid body.</param>
        /// <returns>The rotation of the rigid body with handedness correction applied.</returns>
        public Quaternion GetRotation(int rigidBodyId)
        {
            if (rigidBodyId < 0 || rigidBodyId >= Stream.orientations.Length)
            {
                throw new ArgumentOutOfRangeException(
                    $"{nameof(rigidBodyId)} is out of range of the Stream orientations array.");
            }

            var rotation = Stream.orientations[rigidBodyId];
            var flipCorrection = Config.Polhemus.ChangeHandedness ? -1 : 1;

            // doing crude (90 degree) rotations into frame - Polhemus
            // Applying flip correction here
            return new Quaternion
            {
                w = rotation[0] * flipCorrection,
                x = -rotation[2] * flipCorrection,
                y = rotation[3],
                z = -rotation[1],
            };
        }
    }
}