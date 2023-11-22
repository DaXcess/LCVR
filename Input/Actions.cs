using UnityEngine.InputSystem;

namespace LethalCompanyVR
{
    public class Actions
    {
        public static InputAction XR_HeadPosition = new("Position", binding: "<XRHMD>/centerEyePosition");
        public static InputAction XR_HeadRotation = new("Rotation", binding: "<XRHMD>/centerEyeRotation");
        public static InputAction XR_HeadTrackingState = new("Tracking State", binding: "<XRHMD>/trackingState");

        public static InputAction XR_LeftHand_Thumbstick = new(binding: "<XRController>{LeftHand}/Primary2DAxis");
        public static InputAction XR_RightHand_Thumbstick = new(binding: "<XRController>{RightHand}/Primary2DAxis");

        public static InputAction XR_RightHand_Thumbstick_Y = new(binding: "<XRController>{RightHand}/Primary2DAxis/y", processors: "AxisDeadzone(min=0.75);Invert");

        public static InputAction XR_LeftHand_Thumbstick_Click = new(binding: "<XRController>{LeftHand}/{Primary2DAxisClick}");
        public static InputAction XR_RightHand_Thumbstick_Click = new(binding: "<XRController>{RightHand}/{Primary2DAxisClick}");

        // Buttons are float values, probably because some controllers allow these to be partially pressed
        public static InputAction XR_Button_A = new(binding: "<XRController>{RightHand}/primaryButton");
        public static InputAction XR_Button_B = new(binding: "<XRController>{RightHand}/secondaryButton");
        public static InputAction XR_Button_X = new(binding: "<XRController>{LeftHand}/primaryButton");
        public static InputAction XR_Button_Y = new(binding: "<XRController>{LeftHand}/secondaryButton");

        // Maybe, maybe not, probably will be intercepted by SteamVR anyways
        public static InputAction XR_Menu = new(binding: "<XRController>{LeftHand}/menu");

        static Actions()
        {
            XR_HeadPosition.Enable();
            XR_HeadRotation.Enable();
            XR_HeadTrackingState.Enable();
            
            XR_LeftHand_Thumbstick.Enable();
            XR_RightHand_Thumbstick.Enable();

            XR_RightHand_Thumbstick_Y.Enable();

            XR_LeftHand_Thumbstick_Click.Enable();
            XR_RightHand_Thumbstick_Click.Enable();

            XR_Button_A.Enable();
            XR_Button_B.Enable();
            XR_Button_X.Enable();
            XR_Button_Y.Enable();
        }
    }
}
