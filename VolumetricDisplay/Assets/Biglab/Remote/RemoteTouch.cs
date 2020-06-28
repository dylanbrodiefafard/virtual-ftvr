using UnityEngine;

namespace Biglab.Remote
{
    public struct RemoteTouch
    {
        public readonly int FingerId;
        public readonly Vector2 Position;
        public readonly TouchPhase Phase;
        public readonly Vector2 DeltaPosition;
        public RemoteTouch(Vector2 position, int fingerId, TouchPhase phase, Vector2 deltaPosition) : this()
        {
            Position = position;
            FingerId = fingerId;
            Phase = phase;
            DeltaPosition = deltaPosition;
        }
    }
}
