using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LCVR.Input
{
    internal class SmoothTurningProvider : TurningProvider
    {
        private readonly InputAction turnAction;

        private float offset = 0;

        internal SmoothTurningProvider()
        {
            turnAction = Actions.FindAction("Controls/Turn");
        }

        public void Update()
        {
            var value = turnAction.ReadValue<float>();
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
