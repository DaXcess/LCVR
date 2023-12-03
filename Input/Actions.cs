using UnityEngine.InputSystem;

namespace LethalCompanyVR
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
        public static InputAction XR_RightHand_Thumbstick_Y = new(binding: "<XRController>{RightHand}/Primary2DAxis/y", processors: "AxisDeadzone(min=0.75);Invert");
        public static InputAction XR_RightHand_Thumbstick_Click = new(binding: "<XRController>{RightHand}/{Primary2DAxisClick}");
        public static InputAction XR_RightHand_Grip_Button = new(binding: "<XRController>{RightHand}/gripButton");

        public static InputAction XR_LeftHand_Position = new(binding: "<XRController>{LeftHand}/devicePosition");
        public static InputAction XR_LeftHand_Rotation = new(binding: "<XRController>{LeftHand}/deviceRotation");
        public static InputAction XR_LeftHand_TrackingState = new(binding: "<XRController>{LeftHand}/trackingState");
        public static InputAction XR_LeftHand_IsTracked = new(binding: "<XRController>{LeftHand}/isTracked");
        public static InputAction XR_LeftHand_Thumbstick = new(binding: "<XRController>{LeftHand}/Primary2DAxis");
        public static InputAction XR_LeftHand_Thumbstick_Click = new(binding: "<XRController>{LeftHand}/{Primary2DAxisClick}");

        // Buttons are float values, probably because some controllers allow these to be partially pressed
        public static InputAction XR_Button_A = new(binding: "<XRController>{RightHand}/primaryButton");
        public static InputAction XR_Button_B = new(binding: "<XRController>{RightHand}/secondaryButton");
        public static InputAction XR_Button_X = new(binding: "<XRController>{LeftHand}/primaryButton");
        public static InputAction XR_Button_Y = new(binding: "<XRController>{LeftHand}/secondaryButton");

        // Maybe, maybe not, probably will be intercepted by SteamVR anyways unless something like Oculus or VDXR is being used
        public static InputAction XR_Menu = new(binding: "<XRController>{LeftHand}/menu");

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
            XR_RightHand_Thumbstick_Y.Enable();
            XR_RightHand_Thumbstick_Click.Enable();
            XR_RightHand_Grip_Button.Enable();

            XR_LeftHand_Position.Enable();
            XR_LeftHand_Rotation.Enable();
            XR_LeftHand_TrackingState.Enable();
            XR_LeftHand_IsTracked.Enable();
            XR_LeftHand_Thumbstick.Enable();
            XR_LeftHand_Thumbstick_Click.Enable();

            XR_Button_A.Enable();
            XR_Button_B.Enable();
            XR_Button_X.Enable();
            XR_Button_Y.Enable();
        }
    }
}
