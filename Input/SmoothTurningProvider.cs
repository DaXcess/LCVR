using System;
using UnityEngine;

namespace LCVR.Input
{
    internal class SmoothTurningProvider : TurningProvider
    {
        private float offset = 0;

        public void Update()
        {
            var value = Actions.XR_RightHand_Thumbstick.ReadValue<Vector2>().x;
            bool shouldExecute = MathF.Abs(value) > 0.75;

            if (shouldExecute)
                offset += (value > 0 ? 90 : -90) * Time.deltaTime * Plugin.Config.SmoothTurnSpeedModifier.Value;
        }

        public float GetRotationOffset()
        {
            return offset;
        }
    }
}
