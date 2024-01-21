using System;
using UnityEngine.InputSystem;

namespace LCVR.Input
{
    internal class SnapTurningProvider : TurningProvider
    {
        private readonly InputAction turnAction;
        private readonly float turnAmount;

        private bool turnedLastInput = false;
        private float offset = 0;

        internal SnapTurningProvider()
        {
            turnAction = Actions.FindAction("Controls/Turn");
            turnAmount = Plugin.Config.SnapTurnSize.Value;
        }

        public void Update()
        {
            var value = turnAction.ReadValue<float>();
            bool shouldExecute = MathF.Abs(value) > 0.75;
            
            if (shouldExecute)
            {
                if (turnedLastInput) return;

                turnedLastInput = true;
                offset += value > 0 ? turnAmount : -turnAmount;
            } else
            {
                turnedLastInput = false;
            }
        }

        public float GetRotationOffset()
        {
            return offset;
        }
    }
}
