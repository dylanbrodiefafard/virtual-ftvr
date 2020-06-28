//======================================================================================================
// Copyright 2016, NaturalPoint Inc.
//======================================================================================================

using System;
using System.Linq;
using UnityEngine;


public class OptitrackWiimote : MonoBehaviour
{
    public OptitrackStreamingClient StreamingClient;
    public Int32 RigidBodyId;
    public bool frontClusteredMarkers;



    void Start()
    {
        // If the user didn't explicitly associate a client, find a suitable default.
        if (this.StreamingClient == null)
        {
            this.StreamingClient = OptitrackStreamingClient.FindDefaultClient();

            // If we still couldn't find one, disable this component.
            if (this.StreamingClient == null)
            {
                Debug.LogError(GetType().FullName + ": Streaming client not set, and no " + typeof(OptitrackStreamingClient).FullName + " components found in scene; disabling this component.", this);
                this.enabled = false;
                return;
            }
        }
    }


    void Update()
    {
        OptitrackRigidBodyState rbState = StreamingClient.GetLatestRigidBodyState(RigidBodyId);
        if (rbState != null)
        {

            if (!frontClusteredMarkers)
            {
                //Determine Wiimote position and orientation based on marker positions

                float maxDist = float.MinValue;

                var markers = rbState.Markers.ToArray();

                OptitrackMarkerState marA = markers[0];
                OptitrackMarkerState marB = markers[1];

                foreach (var a in markers)
                {
                    foreach (var b in markers)
                    {
                        float dist = (a.Position - b.Position).magnitude;
                        if (dist > maxDist)
                        {
                            maxDist = dist;
                            marA = a;
                            marB = b;
                        }
                    }
                }

                // Find third
                OptitrackMarkerState marC = markers.First(m => m.Position != marA.Position && m.Position != marB.Position);
                OptitrackMarkerState frontMarker = marA;
                OptitrackMarkerState backMarker = marB;
                if (Vector3.Distance(marB.Position, marC.Position) < Vector3.Distance(marA.Position, marC.Position))
                {
                    frontMarker = marB;
                    backMarker = marA;
                }
                transform.position = frontMarker.Position;
                transform.rotation = Quaternion.LookRotation(frontMarker.Position - backMarker.Position, Vector3.up);
            }
            else
            {
                transform.position = rbState.Pose.Position;
                transform.rotation = rbState.Pose.Orientation;
            }
        }
                           
    }
}
