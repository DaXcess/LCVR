using System.Reflection;
using HarmonyLib;
using LCVR.Patches;
using LCVR.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;

namespace LCVR.Input
{
    public class Actions
    {
        public static InputAction XR_HeadPosition;
        public static InputAction XR_HeadRotation;
        public static InputAction XR_HeadTrackingState;

        public static InputAction XR_RightHand_Position;
        public static InputAction XR_RightHand_Rotation;
        public static InputAction XR_RightHand_TrackingState;
        public static InputAction XR_RightHand_IsTracked = new(binding: "<XRController>{RightHand}/isTracked");
        public static InputAction XR_RightHand_Thumbstick = new(binding: "<XRController>{RightHand}/Primary2DAxis");
        public static InputAction XR_RightHand_Thumbstick_Click = new(binding: "<XRController>{RightHand}/{Primary2DAxisClick}");
        public static InputAction XR_RightHand_Grip_Button = new(binding: "<XRController>{RightHand}/gripButton");
        public static InputAction XR_RightHand_Grip = new(binding: "<XRController>{RightHand}/{Grip}");
        public static InputAction XR_RightHand_Trigger_Button = new(binding: "<XRController>{RightHand}/{TriggerButton}");
        public static InputAction XR_RightHand_Trigger = new(binding: "<XRController>{RightHand}/{Trigger}");

        public static InputAction XR_LeftHand_Position;
        public static InputAction XR_LeftHand_Rotation;
        public static InputAction XR_LeftHand_TrackingState;
        public static InputAction XR_LeftHand_IsTracked = new(binding: "<XRController>{LeftHand}/isTracked");
        public static InputAction XR_LeftHand_Thumbstick = new(binding: "<XRController>{LeftHand}/Primary2DAxis");
        public static InputAction XR_LeftHand_Thumbstick_Click = new(binding: "<XRController>{LeftHand}/{Primary2DAxisClick}");
        public static InputAction XR_LeftHand_Grip_Button = new(binding: "<XRController>{LeftHand}/gripButton");
        public static InputAction XR_LeftHand_Grip = new(binding: "<XRController>{LeftHand}/{Grip}");
        public static InputAction XR_LeftHand_Trigger_Button = new(binding: "<XRController>{LeftHand}/{TriggerButton}");
        public static InputAction XR_LeftHand_Trigger = new(binding: "<XRController>{LeftHand}/{Trigger}");

        // Buttons are float values, probably because some controllers allow these to be partially pressed
        public static InputAction XR_Button_A = new(binding: "<XRController>{RightHand}/primaryButton");
        public static InputAction XR_Button_B = new(binding: "<XRController>{RightHand}/secondaryButton");
        public static InputAction XR_Button_X = new(binding: "<XRController>{LeftHand}/primaryButton");
        public static InputAction XR_Button_Y = new(binding: "<XRController>{LeftHand}/secondaryButton");

        public static InputAction XR_Controller_Position = new(binding: "<XRController>/devicePosition");
        public static InputAction XR_Controller_Rotation = new(binding: "<XRController>/deviceRotation");

        // Maybe, maybe not, probably will be intercepted by SteamVR anyways unless something like Oculus or VDXR is being used
        public static InputAction XR_Menu = new(binding: "<XRController>{LeftHand}/menu");

        public static InputActionAsset InputActions;
        public static InputActionAsset ExperimentalActions;

        static Actions()
        {
            XR_RightHand_IsTracked.Enable();
            XR_RightHand_Thumbstick.Enable();
            XR_RightHand_Thumbstick_Click.Enable();
            XR_RightHand_Grip_Button.Enable();
            XR_RightHand_Grip.Enable();
            XR_RightHand_Trigger_Button.Enable();
            XR_RightHand_Trigger.Enable();

            XR_LeftHand_IsTracked.Enable();
            XR_LeftHand_Thumbstick.Enable();
            XR_LeftHand_Thumbstick_Click.Enable();
            XR_LeftHand_Grip_Button.Enable();
            XR_LeftHand_Grip.Enable();
            XR_LeftHand_Trigger_Button.Enable();
            XR_LeftHand_Trigger.Enable();

            XR_Button_A.Enable();
            XR_Button_B.Enable();
            XR_Button_X.Enable();
            XR_Button_Y.Enable();

            XR_Controller_Position.Enable();
            XR_Controller_Rotation.Enable();

            XR_Button_Y.performed += XR_Button_Y_performed;

            InputActions = InputActionAsset.FromJson(Properties.Resources.inputs);
            InputActions.Enable();

            ExperimentalActions = InputActionAsset.FromJson(Properties.Resources.VRInputs);

            XR_HeadPosition = ExperimentalActions.FindAction("Head/Position");
            XR_HeadRotation = ExperimentalActions.FindAction("Head/Rotation");
            XR_HeadTrackingState = ExperimentalActions.FindAction("Head/Tracking State");

            XR_RightHand_Position = ExperimentalActions.FindAction("Right Hand/Position");
            XR_RightHand_Rotation = ExperimentalActions.FindAction("Right Hand/Rotation");
            XR_RightHand_TrackingState = ExperimentalActions.FindAction("Right Hand/Tracking State");

            XR_LeftHand_Position = ExperimentalActions.FindAction("Left Hand/Position");
            XR_LeftHand_Rotation = ExperimentalActions.FindAction("Left Hand/Rotation");
            XR_LeftHand_TrackingState = ExperimentalActions.FindAction("Left Hand/Tracking State");

            Logger.LogDebug(new InputAction(binding: "<XRHMD>/centerEyeRotation"));
            Logger.LogDebug(ExperimentalActions.FindAction("Head/Position"));
            Logger.LogDebug(ExperimentalActions.FindAction("Head/Rotation"));
            Logger.LogDebug(ExperimentalActions.FindAction("Left Hand/Position"));
            Logger.LogDebug(ExperimentalActions.FindAction("Left Hand/Rotation"));
            Logger.LogDebug(ExperimentalActions.FindAction("Right Hand/Position"));
            Logger.LogDebug(ExperimentalActions.FindAction("Right Hand/Rotation"));

            ExperimentalActions.Enable();
        }

        public static void ReloadInputBindings()
        {
            IngamePlayerSettings.Instance.playerInput.actions = InputActions;

            Logger.LogDebug("Loaded XR input binding overrides");
        }

        private static void XR_Button_Y_performed(InputAction.CallbackContext obj)
        {
            var player = Object.FindObjectOfType<VRPlayer>();

            player?.ResetHeight();

            ReloadInputBindings();
        }
    }

    [LCVRPatch(LCVRPatchTarget.Universal)]
    [HarmonyPatch]
    internal static class IdkPatches
    {
        [HarmonyPatch(typeof(InputActionMap), "SetUpPerActionControlAndBindingArrays")]
        [HarmonyPrefix]
        private static void WhatTheHell(InputActionMap __instance)
        {
            Logger.LogError(__instance.actions[0].bindings[0].path);
        }
    }

    [LCVRPatch(LCVRPatchTarget.Universal)]
    [HarmonyPatch]
    internal static class XRLayoutOnFindLayout_Patches
    {
        private static MethodInfo TargetMethod()
        {
            return AccessTools.TypeByName("UnityEngine.InputSystem.XR.XRLayoutBuilder").GetMethod("OnFindLayoutForDevice", BindingFlags.NonPublic | BindingFlags.Static);
        }

        private static void Postfix(ref InputDeviceDescription description, string matchedLayout)
        {
            Logger.LogDebug($"Found device for layout {matchedLayout}");
        }
    }
}
