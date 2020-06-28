using UnityEngine;

namespace Biglab.Tracking
{
    public abstract class TrackingSubsystem : MonoBehaviour
    {
        public bool IsReady { get; protected set; } = false;

        public abstract Transform TrackingSpace { get; protected set; }

        /// <summary>
        /// Gets the transform of a tracked object with the subsystem id.
        /// </summary>
        public abstract Transform GetTrackingAnchor(int id);
    }
}