using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Biglab.Navigation
{
    /// <summary>
    /// An utility for tracking progress through waypoints.
    /// </summary>
    public class WaypointQueue : IEnumerable<Vector3>
    {
        /// <summary>
        /// The remaining waypoints in the queue.
        /// </summary>
        public IEnumerable<Vector3> Waypoints => _queue;

        /// <summary>
        /// The number of waypoints remaining.
        /// </summary>
        public int Remaining => _queue.Count;

        /// <summary>
        /// The current waypoint target.
        /// </summary>
        public Vector3 Target { get; private set; }

        /// <summary>
        /// Have we claimed every waypoint set?
        /// </summary>
        public bool HasReachedEnd { get; private set; }

        /// <summary>
        /// Event invoked when claiming the last waypoint.
        /// </summary>
        public event Action ReachedEnd;

        private Queue<Vector3> _queue;

        /// <summary>
        /// Creates a new <see cref="WaypointQueue"/>.
        /// </summary>
        public WaypointQueue()
        {
            _queue = new Queue<Vector3>();
        }

        /// <summary>
        /// Claim that we have visited the target waypoint.
        /// </summary>
        public void ClaimTargetWaypoint()
        {
            // If we have waypoints remaining
            if (Remaining > 0)
            {
                // Get next target
                Target = _queue.Dequeue();
            }
            else
            {
                // Trigger end event
                if (!HasReachedEnd)
                {
                    HasReachedEnd = true;
                    ReachedEnd?.Invoke();
                }
            }
        }

        public void Clear()
        {
            _queue.Clear();
            HasReachedEnd = false;
            Target = Vector3.zero;
        }

        /// <summary>
        /// Set the waypoints ( resets the reached end status ).
        /// </summary>
        /// <param name="points"></param>
        public void SetWaypoints(IEnumerable<Vector3> points)
        {
            // Clear previous waypoints
            _queue.Clear();

            // Add enqueue new points
            foreach (var pt in points)
            {
                _queue.Enqueue(pt);
            }

            // If we are given at least one way-point
            if (_queue.Count > 0)
            {
                // Get first way-point
                Target = _queue.Dequeue();
            }

            // Clear end flag
            HasReachedEnd = false;
        }

        public IEnumerator<Vector3> GetEnumerator()
        {
            yield return Target;
            foreach (var pt in _queue)
            {
                yield return pt;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<Vector3>)_queue).GetEnumerator();
        }
    }
}