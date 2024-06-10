using System;
using UnityEngine;

namespace LCVR.Input;

internal class SmoothTurningProvider : TurningProvider
{
    private float offset;

    public void Update()
    {
        var value = Actions.Instance["Turn"].ReadValue<float>();
        var shouldExecute = MathF.Abs(value) > 0.75;

        if (!shouldExecute)
            return;

        var totalRotation = (value > 0 ? 90 : -90) * Time.deltaTime * Plugin.Config.SmoothTurnSpeedModifier.Value;

        offset += totalRotation;
    }

    public void SetOffset(float offset)
    {
        this.offset = offset;
    }

    public float GetRotationOffset()
    {
        return offset;
    }
}
