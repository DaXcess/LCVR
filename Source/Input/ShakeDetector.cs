using System;
using UnityEngine;

namespace LCVR.Input;

public class ShakeDetector(Transform source, float threshold, bool useLocalPosition = false)
{
    private const float AccelerometerUpdateInterval = 1.0f / 60.0f;
    private const float LowPassKernelWidthInSeconds = 1.0f;
    
    public Action onShake;

    private float shakeDetectionThreshold = threshold * threshold;
    private float lowPassFilterFactor = AccelerometerUpdateInterval / LowPassKernelWidthInSeconds;
    private Vector3 lowPassValue;
    private Vector3 previousPosition = useLocalPosition ? source.localPosition : source.position;
    
    private Vector3 Position => useLocalPosition ? source.localPosition : source.position;

    public void Update()
    {
        var acceleration = Position - previousPosition;
        lowPassValue = Vector3.Lerp(lowPassValue, acceleration, lowPassFilterFactor);
        var deltaAcceleration = acceleration - lowPassValue;

        if (deltaAcceleration.sqrMagnitude >= shakeDetectionThreshold)
            onShake?.Invoke();

        previousPosition = Position;
    }
}
