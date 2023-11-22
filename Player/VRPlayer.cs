using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;

namespace LethalCompanyVR.Player
{
    public class VRPlayer
    {
        // Attempt to assemble an XR Rig using current Player game objects
        public static void InitializeXRRig()
        {
            // Find the "Player" game object
            GameObject player = GameObject.Find("Player");

            // Check if the player object is found
            if (player != null)
            {
                // Add XR components to the player object
                var xrRig = player.GetComponent<XROrigin>();
                if (xrRig == null)
                {
                    xrRig = player.AddComponent<XROrigin>();
                }

                xrRig.CameraFloorOffsetObject = player;
                xrRig.Origin = player;
                xrRig.Camera = CameraPatches.activeCamera;

                // Add locomotion system and other components
                var locomotionSystem = player.AddComponent<LocomotionSystem>();
                var snapTurnProvider = player.AddComponent<ActionBasedSnapTurnProvider>();

                // Making component work
                snapTurnProvider.enabled = true;
                snapTurnProvider.leftHandSnapTurnAction = new InputActionProperty(Actions.XR_RightHand_Thumbstick);
                snapTurnProvider.enableTurnLeftRight = true;
                snapTurnProvider.turnAmount = 25f;
                snapTurnProvider.debounceTime = .05f;
                snapTurnProvider.system = locomotionSystem;

                locomotionSystem.enabled = true;
                locomotionSystem.xrOrigin = xrRig;

                xrRig.enabled = true;

            }
            else
            {
                return;
            }
        }
    }
}
