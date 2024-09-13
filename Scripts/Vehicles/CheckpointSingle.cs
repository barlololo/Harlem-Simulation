using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// checkpoint on the route and checks if the agent driver is following the correct route
/// </summary>
public class CheckpointSingle : MonoBehaviour
{
    private TrackCheckpoints trackCheckpoints;

    /// <summary>
    /// when the car passes through the checkpoint,
    /// invoking events on whether the car enters the right checkpoint
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<CarDriverAgent>(out CarDriverAgent carDriverAgent))
        {
            if (carDriverAgent.GetAssignedTrackCheckpoints() == trackCheckpoints)   //car and track both match
            {
                trackCheckpoints.CarThroughCheckpoint(this, other.transform.parent);
            }
        }
    }

    /// <summary>
    /// relate each checkpoint to the whole system
    /// so that each checkpoint could have the ability to use CarThroughCheckpoint() for test
    /// </summary>
    /// <param name="trackCheckpoints"></param>
    public void SetTrackCheckpoints(TrackCheckpoints trackCheckpoints)
    {
        this.trackCheckpoints = trackCheckpoints;
    }
}
