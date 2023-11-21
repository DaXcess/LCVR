using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

namespace LethalCompanyVR
{
    [HarmonyPatch]
    public class CameraPatches
    {
        public static Camera activeCamera = null;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SwitchCamera))]
        private static void OnCameraSwitched()
        {
            Logger.LogWarning("SwitchCamera called !!!");

            activeCamera = StartOfRound.Instance.activeCamera;

            if (activeCamera == null)
            {
                Logger.LogError("Where is StartOfRound.activeCamera?!?!?");
                return;
            }

            if (Plugin.VR_ENABLED)
            {
                // TODO: I'm guessing this ain't always the correct FOV
                activeCamera.fieldOfView = 98.04017f;
                activeCamera.targetDisplay = 1;
                activeCamera.stereoTargetEye = StereoTargetEyeMask.Both;

                // TODO: Check if HMD tracking can be done better
                var driver = activeCamera.gameObject.AddComponent<CameraPoseDriver>();

                driver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
                driver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;

                driver.positionAction = Actions.XR_HeadPosition;
                driver.rotationAction = Actions.XR_HeadRotation;
                driver.trackingStateInput = new InputActionProperty(Actions.XR_HeadTrackingState);

                var player = GameObject.Find("Player");

                var playerDriver = player.gameObject.AddComponent<PlayerPoseDriver>();

                playerDriver.trackingType = TrackedPoseDriver.TrackingType.RotationOnly;
                playerDriver.updateType = TrackedPoseDriver.UpdateType.BeforeRender;
                playerDriver.rotationAction = Actions.XR_HeadRotation;

                // TODO: Clean up
                var helmet = GameObject.Find("PlayerHUDHelmetModel");

                if (helmet)
                {
                    helmet.SetActive(false);
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.SpawnPlayerAnimation))]
        private static bool OnPlayerSpawnAnimation()
        {
            if (!Plugin.VR_ENABLED) return true;

            Logger.Log("PlayerControllerB.SpawnPlayerAnimation called");

            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB), "Update")]
        private static void OnTick()
        {
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Camera), "fieldOfView")]
        [HarmonyPatch(MethodType.Setter)]
        private static void OnSetFieldOfView()
        {
            var st = new System.Diagnostics.StackTrace();

            Logger.LogDebug("Camera FOV Updated");
            Logger.LogDebug(st.ToString());
        }
    }
}
