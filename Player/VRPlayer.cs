using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;

namespace LethalCompanyVR.Player
{
    public class VRPlayer
    {
        /// <summary>
        /// Initialize an XR Rig on the Player game object
        /// </summary>
        public static void InitializeXRRig()
        {
            // Find the "Player" game object
            GameObject player = GameObject.Find("Player");

            // Check if the player object is found
            if (player == null)
            {
                Logger.LogError("Could not find player object");
                return;
            }

            // Add XR components to the player object
            var xrRig = player.GetComponent<XROrigin>() ?? player.AddComponent<XROrigin>();
            xrRig.CameraFloorOffsetObject = player;
            xrRig.Origin = player;
            xrRig.Camera = Plugin.MainCamera;

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

            Logger.LogDebug("XR Rig has been created");
        }
    }
}
