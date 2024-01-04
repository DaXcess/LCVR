using LCVR.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LCVR.Input
{
    public class Actions
    {
        public static InputAction Head_Position;
        public static InputAction Head_Rotation;
        public static InputAction Head_TrackingState;

        public static InputAction RightHand_Position;
        public static InputAction RightHand_Rotation;
        public static InputAction RightHand_TrackingState;

        public static InputAction LeftHand_Position;
        public static InputAction LeftHand_Rotation;
        public static InputAction LeftHand_TrackingState;

        public static readonly InputActionAsset VRInputActions;
        private static readonly InputActionAsset LCInputActions;

        static Actions()
        {
            LCInputActions = InputActionAsset.FromJson(Properties.Resources.lc_inputs);
            VRInputActions = InputActionAsset.FromJson(Properties.Resources.vr_inputs);

            Head_Position = VRInputActions.FindAction("Head/Position");
            Head_Rotation = VRInputActions.FindAction("Head/Rotation");
            Head_TrackingState = VRInputActions.FindAction("Head/Tracking State");

            RightHand_Position = VRInputActions.FindAction("Right Hand/Position");
            RightHand_Rotation = VRInputActions.FindAction("Right Hand/Rotation");
            RightHand_TrackingState = VRInputActions.FindAction("Right Hand/Tracking State");

            LeftHand_Position = VRInputActions.FindAction("Left Hand/Position");
            LeftHand_Rotation = VRInputActions.FindAction("Left Hand/Rotation");
            LeftHand_TrackingState = VRInputActions.FindAction("Left Hand/Tracking State");

            LCInputActions.Enable();
            VRInputActions.Enable();
        }

        public static void ReloadInputBindings()
        {
            IngamePlayerSettings.Instance.playerInput.actions = LCInputActions;

            Logger.LogDebug("Loaded XR input binding overrides");
        }
    }
}
