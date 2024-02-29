﻿using System;

namespace LCVR.Input;

internal class SnapTurningProvider : TurningProvider
{
    private bool turnedLastInput = false;
    private float offset = 0;

    public void Update()
    {
        var value = Actions.Instance["Controls/Turn"].ReadValue<float>();
        bool shouldExecute = MathF.Abs(value) > 0.75;

        if (shouldExecute)
        {
            var turnAmount = Plugin.Config.SnapTurnSize.Value;
            if (turnedLastInput) return;

            turnedLastInput = true;
            offset += value > 0 ? turnAmount : -turnAmount;
        }
        else
        {
            turnedLastInput = false;
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
