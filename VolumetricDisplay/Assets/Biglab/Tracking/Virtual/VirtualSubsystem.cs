using Biglab.Extensions;
using System.Collections;
using System.Collections.Generic;
using Biglab.Utility.Transforms;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Biglab.Tracking.Virtual
{
    public class VirtualSubsystem : TrackingSubsystem
    {
        public override Transform TrackingSpace { get; protected set; }

        private Dictionary<int, Transform> _virtualAnchors;

        private Transform _displayInTracking;

        #region MonoBehaviour

        private void Awake()
        {
            _virtualAnchors = new Dictionary<int, Transform>();
            TrackingSpace = transform; // Track locally

            _displayInTracking = new GameObject("Display in Tracking").transform;
            _displayInTracking.parent = transform;

            // Subscribe to TrackingToPhysicalChanged event to update the display in tracking transform
            TrackingSystem.Instance.TrackingToPhysicalChanged += OnTrackingToPhysicalChanged;

            // Subscribe to scene changes to handle looking for virtual tracked objects in the new scene
            SceneManager.sceneUnloaded += SceneUnloaded;
            SceneManager.activeSceneChanged += ActiveSceneChanged;
        }

        private IEnumerator Start()
        {
            // Wait for everything in the scene to get setup
            yield return null;

            // Find all of the virtual objects in the scene
            FindVirtualObjectsInScene();

            // Subsystem is setup and ready to go
            IsReady = true;
        }

        private void OnDestroy()
        {
            if (TrackingSystem.Instance != null)
            {
                // unsubscribe from the event before we are destroyed
                TrackingSystem.Instance.TrackingToPhysicalChanged -= OnTrackingToPhysicalChanged;
            }
        }

        #endregion

        private void SceneUnloaded(Scene scene)
        {
            if (scene.path == SceneManager.GetActiveScene().path)
            {
                _virtualAnchors.Clear();
            }
        }

        private void ActiveSceneChanged(Scene prev, Scene current)
        {
            IsReady = false;

            FindVirtualObjectsInScene();

            IsReady = true;
        }

        private void FindVirtualObjectsInScene()
        {
            // Find all virtual tracked objects in the scene
            var trackedObjects = FindObjectsOfType<VirtualTrackedObject>();

            // Store each already known tracked object
            foreach (var trackedObject in trackedObjects)
            {
                _virtualAnchors[trackedObject.Id] = trackedObject.transform;
            }
        }

        /// <summary>
        /// Handles the TrackingToPhysicalChanged event.
        /// Updates the display in tracking to match the updated calibration
        /// </summary>
        public void OnTrackingToPhysicalChanged()
        {
            var physicalToTracking = TrackingSystem.Instance.TrackingToPhysical.TrackerToDisplayTransformation.inverse;
            // TODO: make sure this code is correct
            _displayInTracking.localPosition = physicalToTracking.MultiplyPoint3x4(Vector3.zero);
            _displayInTracking.localRotation = physicalToTracking.rotation;
            _displayInTracking.localScale = physicalToTracking.ToScale();
        }

        public override Transform GetTrackingAnchor(int id)
        {
            if (!_virtualAnchors.ContainsKey(id) || _virtualAnchors[id] == null)
            {
                var trackedDummy = CreateDummyTrackedObject(id);
                _virtualAnchors[trackedDummy.Id] = trackedDummy.transform;
            }

            return _virtualAnchors[id];
        }

        private VirtualTrackedObject CreateDummyTrackedObject(int id)
        {
            var kind = TrackedObject.GetTrackedObjectKind(id);
            var index = TrackedObject.GetTrackedObjectIndex(id);

            var dummyGameObject = new GameObject($"Virtual Tracked Object (Dummy {kind} {index})");

            if (_displayInTracking != null)
            {
                // Put around the display
                dummyGameObject.transform.parent = _displayInTracking;
                dummyGameObject.transform.localPosition = new Vector3{
                    x = Random.value.Rescale(0, 1, -1, 1),
                    y = 0.5f,
                    z = Random.value.Rescale(0, 1, -1, 1)
                };

                // Have it look at the display
                dummyGameObject.AddComponentWithInit<LookAtTarget>(script =>
                {
                    script.IsInterpolated = true;
                    script.InterpolationSpeed = 2;
                    script.Target = _displayInTracking;
                    script.Up = LookAtTarget.UpDirection.Target;
                });

                // Have it rotate about the display
                dummyGameObject.AddComponentWithInit<RotateAboutPosition>(script =>
                {
                    script.LocalSpace = _displayInTracking;
                    script.LocalAxis = Vector3.up;
                    script.RotationSpeed = 20;
                });
            }

            // Let's say we are tracking it
            return dummyGameObject.AddComponentWithInit<VirtualTrackedObject>(script =>
            {
                script.ObjectKind = kind;
                script.ObjectIndex = index;
            });
        }
    }
}