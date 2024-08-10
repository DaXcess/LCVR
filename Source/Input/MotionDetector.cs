using UnityEngine;
using UnityEngine.Events;

namespace LCVR.Input;

public class MotionDetector : MonoBehaviour
{
    public UnityEvent onShake;

    private Vector3 lastPosition;

    private float shakeThreshold = 0.5f;
    private float shakeHoldThreshold = 0.09f;
    private float shakeDelay = 0.2f;

    private float lastShakeTime = 0;
    private bool startHold = false;

    private void Awake()
    {
        onShake = new UnityEvent();
    }

    private void FixedUpdate()
    {
        var distance = Vector3.Distance(lastPosition, transform.localPosition) * 10;

        if (startHold && distance > shakeHoldThreshold)
            TriggerShake();
        else
            startHold = false;

        if (distance > shakeThreshold)
            startHold = true;

        lastPosition = transform.localPosition;
    }

    private void TriggerShake()
    {
        if (Time.realtimeSinceStartup - lastShakeTime < shakeDelay)
            return;

        lastShakeTime = Time.realtimeSinceStartup;

        onShake.Invoke();
    }
}
