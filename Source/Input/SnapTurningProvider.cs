using System;

namespace LCVR.Input;

internal class SnapTurningProvider : TurningProvider
{
    private bool turnedLastInput;
    private float offset;

    public float Update()
    {
        var value = Actions.Instance["Turn"].ReadValue<float>();
        var shouldExecute = MathF.Abs(value) > 0.75;

        if (shouldExecute)
        {
            var turnAmount = Plugin.Config.SnapTurnSize.Value;
            if (turnedLastInput) return 0;

            turnAmount = value > 0 ? turnAmount : -turnAmount;
            turnedLastInput = true;
            offset += turnAmount;

            return turnAmount;
        }

        turnedLastInput = false;

        return 0;
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
