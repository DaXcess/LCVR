using UnityEngine.InputSystem;

namespace LethalCompanyVR.Input
{
    public class Actions
    {
        public static InputAction XR_HeadPosition = new("Position", binding: "<XRHMD>/centerEyePosition");
        public static InputAction XR_HeadRotation = new("Rotation", binding: "<XRHMD>/centerEyeRotation");
        public static InputAction XR_HeadTrackingState = new("Tracking State", binding: "<XRHMD>/trackingState");

        // TODO: Make these work somehow
        public static InputAction XR_LeftHand_Something = new(binding: "<XRController>{LeftHand}/thumbstick");
        public static InputAction XR_RightHand_Something = new(binding: "<XRController>{RightHand}/thumbstick");
    }
}
