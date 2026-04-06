using LCVR.Player;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace LCVR.Physics.Interactions;

/// <summary>
/// Tracks hand velocity for VR interactors to detect slap gestures
/// </summary>
internal class VelocityTracker : MonoBehaviour
{
    /// <summary>
    /// Event fired when a tracked hand's velocity exceeds the configured threshold
    /// </summary>
    public event Action<VRInteractor, Vector3> OnVelocityThresholdExceeded;

    private Dictionary<VRInteractor, HandVelocityData> trackedHands = new();
    private float velocityThreshold = 2.0f;

    /// <summary>
    /// Gets or sets the velocity threshold for slap detection (in m/s)
    /// </summary>
    public float VelocityThreshold
    {
        get => velocityThreshold;
        set => velocityThreshold = value;
    }

    private void Update()
    {
        // Update velocity tracking for all tracked hands
        foreach (var interactor in trackedHands.Keys)
        {
            UpdateHandVelocity(interactor);
        }
    }

    /// <summary>
    /// Starts tracking velocity for the specified VR interactor
    /// </summary>
    public void StartTracking(VRInteractor interactor)
    {
        if (interactor == null)
            return;

        if (trackedHands.ContainsKey(interactor))
            return;

        var xrNode = interactor.IsRightHand ? XRNode.RightHand : XRNode.LeftHand;
        
        trackedHands[interactor] = new HandVelocityData
        {
            interactor = interactor,
            xrNode = xrNode,
            currentVelocity = Vector3.zero,
            lastCheckTime = Time.time,
            thresholdExceeded = false
        };
    }

    /// <summary>
    /// Stops tracking velocity for the specified VR interactor
    /// </summary>
    public void StopTracking(VRInteractor interactor)
    {
        if (interactor == null)
            return;

        trackedHands.Remove(interactor);
    }

    /// <summary>
    /// Gets the current velocity for the specified VR interactor
    /// </summary>
    public Vector3 GetCurrentVelocity(VRInteractor interactor)
    {
        if (interactor == null || !trackedHands.ContainsKey(interactor))
            return Vector3.zero;

        return trackedHands[interactor].currentVelocity;
    }

    /// <summary>
    /// Checks if the specified VR interactor is currently being tracked
    /// </summary>
    public bool IsTracking(VRInteractor interactor)
    {
        return interactor != null && trackedHands.ContainsKey(interactor);
    }

    private void UpdateHandVelocity(VRInteractor interactor)
    {
        if (!trackedHands.ContainsKey(interactor))
            return;

        var data = trackedHands[interactor];
        
        // Query XR device for velocity
        var device = InputDevices.GetDeviceAtXRNode(data.xrNode);
        
        if (!device.isValid)
        {
            Logger.LogWarning($"XR device not found for node {data.xrNode}");
            data.currentVelocity = Vector3.zero;
            trackedHands[interactor] = data;
            return;
        }

        if (!device.TryGetFeatureValue(CommonUsages.deviceVelocity, out Vector3 velocity))
        {
            Logger.LogWarning($"Device velocity not supported for {data.xrNode}");
            data.currentVelocity = Vector3.zero;
            trackedHands[interactor] = data;
            return;
        }

        data.currentVelocity = velocity;
        data.lastCheckTime = Time.time;

        // Check if velocity exceeds threshold
        var currentlyExceeded = velocity.magnitude > velocityThreshold;
        
        // Emit event on rising edge (threshold crossed from below to above)
        if (currentlyExceeded && !data.thresholdExceeded)
        {
            OnVelocityThresholdExceeded?.Invoke(interactor, velocity);
        }
        
        data.thresholdExceeded = currentlyExceeded;
        trackedHands[interactor] = data;
    }
}

/// <summary>
/// Data structure for tracking hand velocity state
/// </summary>
internal struct HandVelocityData
{
    public VRInteractor interactor;
    public XRNode xrNode;
    public Vector3 currentVelocity;
    public float lastCheckTime;
    public bool thresholdExceeded;
}
