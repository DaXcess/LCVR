using UnityEngine;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;

namespace LethalCompanyVR.Player
{
    public class VRPlayer
    {
        /// <summary>
        /// Initialize an XR Rig on the Player game object
        /// </summary>
        public static void InitializeXRRig()
        {
            if (!Plugin.VR_ENABLED) return;

            // Find the "Player" game object
            GameObject player = GameObject.Find("Player");

            // Check if the player object is found
            if (player == null)
            {
                Logger.LogError("Could not find player object");
                return;
            }

          
            var driver = Plugin.MainCamera.gameObject.AddComponent<CameraPoseDriver>();
            driver.trackingType = TrackedPoseDriver.TrackingType.RotationOnly;
            driver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;

            driver.positionAction = Actions.XR_HeadPosition;
            driver.rotationAction = Actions.XR_HeadRotation;
            driver.trackingStateInput = new InputActionProperty(Actions.XR_HeadTrackingState);

            driver.playerTransform = player.transform;

            var camOffset = new GameObject("CameraOffset");

            // INITIALIZE XR ORIGIN
            var xrRig = player.GetComponent<XROrigin>() ?? player.AddComponent<XROrigin>();
            xrRig.CameraFloorOffsetObject = camOffset;
            xrRig.Origin = player;
            xrRig.Camera = Plugin.MainCamera;
            xrRig.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device;
            xrRig.enabled = true;

            var leftControllerGO = new GameObject("LeftController");
            leftControllerGO.transform.parent = player.transform;
            leftControllerGO.AddComponent<MeshRenderer>(); // Add a visual representation if needed

            var rightControllerGO = new GameObject("RightController");
            rightControllerGO.transform.parent = player.transform;
            rightControllerGO.AddComponent<MeshRenderer>(); // Add a visual representation if needed

            Logger.LogDebug("XR Rig has been created");
        }

        public static void VibrateController(XRNode hand, float duration, float amplitude)
        {
            UnityEngine.XR.InputDevice device = InputDevices.GetDeviceAtXRNode(hand);

            if (device != null && device.TryGetHapticCapabilities(out HapticCapabilities capabilities) && capabilities.supportsImpulse)
            {
                device.SendHapticImpulse(0, amplitude, duration);
            }
        }
    }
}
