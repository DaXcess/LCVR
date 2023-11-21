using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

namespace LethalCompanyVR
{
    public class Actions
    {
        public static InputAction XR_HeadPosition = new("Position", binding: "<XRHMD>/centerEyePosition");
        public static InputAction XR_HeadRotation = new("Rotation", binding: "<XRHMD>/centerEyeRotation");
        public static InputAction XR_HeadTrackingState = new("Tracking State", binding: "<XRHMD>/trackingState");

        public static InputAction XR_LeftHand_Thumbstick = new(binding: "<XRController>{LeftHand}/Primary2DAxis");
        public static InputAction XR_RightHand_Thumbstick = new(binding: "<XRController>{RightHand}/Primary2DAxis");

        public static InputAction XR_Button_A = new(binding: "<XRController>{RightHand}/primaryButton");
        public static InputAction XR_Button_B = new(binding: "<XRController>{RightHand}/secondaryButton");
        public static InputAction XR_Button_X = new(binding: "<XRController>{LeftHand}/primaryButton");
        public static InputAction XR_Button_Y = new(binding: "<XRController>{LeftHand}/secondaryButton");

        // Maybe, maybe not, probably will be intercepted by SteamVR anyways
        public static InputAction XR_Menu = new(binding: "<XRController>{LeftHand}/menu");
    }
}
