using LCVR.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LCVR.Input
{
    public class Actions
    {
        public static InputAction XR_HeadPosition = new("Position", binding: "<XRHMD>/centerEyePosition");
        public static InputAction XR_HeadRotation = new("Rotation", binding: "<XRHMD>/centerEyeRotation");
        public static InputAction XR_HeadTrackingState = new("Tracking State", binding: "<XRHMD>/trackingState");

        public static InputAction XR_RightHand_Position = new(binding: "<XRController>{RightHand}/devicePosition");
        public static InputAction XR_RightHand_Rotation = new(binding: "<XRController>{RightHand}/deviceRotation");
        public static InputAction XR_RightHand_TrackingState = new(binding: "<XRController>{RightHand}/trackingState");
        public static InputAction XR_RightHand_IsTracked = new(binding: "<XRController>{RightHand}/isTracked");
        public static InputAction XR_RightHand_Thumbstick = new(binding: "<XRController>{RightHand}/Primary2DAxis");
        public static InputAction XR_RightHand_Thumbstick_Click = new(binding: "<XRController>{RightHand}/{Primary2DAxisClick}");
        public static InputAction XR_RightHand_Grip_Button = new(binding: "<XRController>{RightHand}/gripButton");
        public static InputAction XR_RightHand_Grip = new(binding: "<XRController>{RightHand}/{Grip}");
        public static InputAction XR_RightHand_Trigger_Button = new(binding: "<XRController>{RightHand}/{TriggerButton}");
        public static InputAction XR_RightHand_Trigger = new(binding: "<XRController>{RightHand}/{Trigger}");

        public static InputAction XR_LeftHand_Position = new(binding: "<XRController>{LeftHand}/devicePosition");
        public static InputAction XR_LeftHand_Rotation = new(binding: "<XRController>{LeftHand}/deviceRotation");
        public static InputAction XR_LeftHand_TrackingState = new(binding: "<XRController>{LeftHand}/trackingState");
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

        static Actions()
        {
            XR_HeadPosition.Enable();
            XR_HeadRotation.Enable();
            XR_HeadTrackingState.Enable();

            XR_RightHand_Position.Enable();
            XR_RightHand_Rotation.Enable();
            XR_RightHand_TrackingState.Enable();
            XR_RightHand_IsTracked.Enable();
            XR_RightHand_Thumbstick.Enable();
            XR_RightHand_Thumbstick_Click.Enable();
            XR_RightHand_Grip_Button.Enable();
            XR_RightHand_Grip.Enable();
            XR_RightHand_Trigger_Button.Enable();
            XR_RightHand_Trigger.Enable();

            XR_LeftHand_Position.Enable();
            XR_LeftHand_Rotation.Enable();
            XR_LeftHand_TrackingState.Enable();
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
}
