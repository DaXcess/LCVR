using System;
using UnityEngine;

namespace LCVR.Input
{
    internal class SnapTurningProvider : TurningProvider
    {
        private bool turnedLastInput = false;
        private float offset = 0;

        public void Update()
        {
            var value = Actions.XR_RightHand_Thumbstick.ReadValue<Vector2>().x;
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
