using System;
using UnityEngine;

namespace LCVR.Input;

internal class SmoothTurningProvider : TurningProvider
{
    private float offset = 0;

    public void Update()
    {
        var value = Actions.Instance["Controls/Turn"].ReadValue<float>();
        bool shouldExecute = MathF.Abs(value) > 0.75;

        if (shouldExecute)
        {
            var totalRotation = (value > 0 ? 90 : -90) * Time.deltaTime * Plugin.Config.SmoothTurnSpeedModifier.Value;

            offset += totalRotation;
        }
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
