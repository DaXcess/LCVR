using UnityEngine;

namespace LCVR.Items
{
    internal class VRFlashlight : VRItem<FlashlightItem>
    {
        private Vector3 positionOffset = new(0.01f, 0.2f, -0.065f);
        private Vector3 rotationOffset = new(-90, 0, 0);

        protected new void Awake()
        {
            base.Awake();

            UpdateWhenPocketed = true;
        }

        protected override void OnUpdate()
        {
            if (IsLocal)
                return;

            var isHoldingActiveFlashlight = (player.currentlyHeldObjectServer?.itemProperties.itemId == 1 || player.currentlyHeldObjectServer?.itemProperties.itemId == 6)
                                                && player.currentlyHeldObjectServer.isBeingUsed;
            // currentlyHeldObjectServer is guaranteed to not be null at this point

            if (!item.isPocketed)
            {
                // Update flashlight offsets

                player.allHelmetLights[0].transform.ApplyOffsetTransform(networkPlayer.rightHandTarget, positionOffset, rotationOffset);
                player.allHelmetLights[1].transform.ApplyOffsetTransform(networkPlayer.rightHandTarget, positionOffset, rotationOffset);
                player.allHelmetLights[2].transform.ApplyOffsetTransform(networkPlayer.rightHandTarget, positionOffset, rotationOffset);
            }
            else if (!isHoldingActiveFlashlight)
            {
                // We don't want to run this code if the player is holding another flashlight

                player.allHelmetLights[0].transform.localPosition = new Vector3(0.207f, -0.526f, 0.475f);
                player.allHelmetLights[0].transform.localEulerAngles = new Vector3(0, 357.6089f, 0);

                player.allHelmetLights[1].transform.localPosition = new Vector3(0.207f, -0.526f, 0.475f);
                player.allHelmetLights[1].transform.localEulerAngles = new Vector3(0, 357.6089f, 0);

                player.allHelmetLights[2].transform.localPosition = new Vector3(0.207f, -0.526f, 0.475f);
                player.allHelmetLights[2].transform.localEulerAngles = new Vector3(0, 357.6089f, 0);
            }
        }
    }
}
