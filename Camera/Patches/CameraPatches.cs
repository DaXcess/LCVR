using GameNetcodeStuff;
using HarmonyLib;
using LethalCompanyVR.Player;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

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
            VRPlayer.InitializeXRRig();
            if (!Plugin.VR_ENABLED) return;

            Logger.LogWarning("SwitchCamera called !!!");

            activeCamera = StartOfRound.Instance.activeCamera;

            if (activeCamera == null)
            {
                Logger.LogError("Where is StartOfRound.activeCamera?!?!?");
                return;
            }

            // TODO: I'm guessing this ain't always the correct FOV
            if (Plugin.RenderCamera != null)
            {
                activeCamera.fieldOfView = Plugin.RenderCamera.fieldOfView;
            }

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

            // TODO: Keep helmet model once we know how to render it properly
            var helmet = GameObject.Find("PlayerHUDHelmetModel");

            if (helmet)
            {
                helmet.SetActive(false);
            }

            // TODO: Oh god why did they make it like this
            var ui = GameObject.Find("UI");
            var hud = GameObject.Find("IngamePlayerHUD");
            //var canvas = hud.GetComponentInParent<Canvas>();

            if (hud == null || ui == null)
            {
                Logger.LogError("Failed to find HUD, game will look weird");
                return;
            }

            var canvasObject = new GameObject("HUDCanvas");
            var canvas = canvasObject.AddComponent<Canvas>();

            hud.transform.parent = canvasObject.transform;

            var imageObject = new GameObject("RedSquare");
            imageObject.transform.parent = hud.transform;

            var image = imageObject.AddComponent<Image>();
            image.color = Color.red;

            var rect = image.GetComponent<RectTransform>();
            rect.localPosition = new Vector3(0, 0, 0);
            rect.sizeDelta = new Vector2(480, 480);

            Plugin.MainCamera = activeCamera;

            AttachedUI.Create(canvas, 0.00085f);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.SpawnPlayerAnimation))]
        private static bool OnPlayerSpawnAnimation()
        {
            if (!Plugin.VR_ENABLED) return true;

            Logger.Log("PlayerControllerB.SpawnPlayerAnimation called");

            return false;
        }
    }
}
