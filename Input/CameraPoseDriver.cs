using UnityEngine;
using UnityEngine.InputSystem.XR;

namespace LethalCompanyVR
{
    // TODO: Check if we can ditch this hacky approach
    public class CameraPoseDriver : TrackedPoseDriver
    {
        public Transform playerTransform;

        private bool justTurned = false;

        public float rotationOffset = 0;

        void Update()
        {
            HandleSnapTurn();
        }

        private void HandleSnapTurn()
        {
            var input = Actions.XR_RightHand_Thumbstick.ReadValue<Vector2>();

            if (input.x > 0.75f)
            {
                if (justTurned) return;

                justTurned = true;
                rotationOffset = (rotationOffset + 45) % 360;
            }
            else if (input.x < -0.75f)
            {
                if (justTurned) return;

                justTurned = true;
                rotationOffset = (rotationOffset - 45) % 360;
            }
            else
            {
                justTurned = false;
            }
        }

        protected override void SetLocalTransform(Vector3 newPosition, Quaternion newRotation)
        {
            base.SetLocalTransform(newPosition, Quaternion.Euler(newRotation.eulerAngles.x, 0, newRotation.eulerAngles.z));

            playerTransform.rotation = Quaternion.Euler(playerTransform.rotation.eulerAngles.x, newRotation.eulerAngles.y + rotationOffset, playerTransform.rotation.eulerAngles.z);
        }
    }
}
