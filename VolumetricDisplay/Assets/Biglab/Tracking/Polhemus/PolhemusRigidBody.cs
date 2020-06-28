using Biglab.Extensions;
using UnityEngine;

namespace Biglab.Tracking.Polhemus
{
    public class PolhemusRigidBody : MonoBehaviour
    {
        public PolhemusSubsystem TrackingSubsystem;
        public int RigidBodyId;

        void Start()
        {
            if (TrackingSubsystem.IsNull())
            {
                TrackingSubsystem = FindObjectOfType<PolhemusSubsystem>(); // Try and find the subsystem
            }
        }

        void Update()
        {
            // Check that the polhemus tracker is setup and initialized.
            if (TrackingSubsystem.IsNull() || !TrackingSubsystem.IsReady)
            {
                return;
            }

            transform.position = TrackingSubsystem.GetPosition(RigidBodyId);
            transform.rotation = TrackingSubsystem.GetRotation(RigidBodyId);
        }
    }
}