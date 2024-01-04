using System;
using UnityEngine.InputSystem;

namespace LCVR.Input
{
    internal class SnapTurningProvider : TurningProvider
    {
        private readonly InputAction turnAction;

        private bool turnedLastInput = false;
        private float offset = 0;

        internal SnapTurningProvider()
        {
            turnAction = Actions.VRInputActions.FindAction("Controls/Turn");
        }

        public void Update()
        {
            var value = turnAction.ReadValue<float>();
            bool shouldExecute = MathF.Abs(value) > 0.75;
            
            if (shouldExecute)
            {
                if (turnedLastInput) return;

                turnedLastInput = true;
                offset += value > 0 ? 45 : -45;
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
