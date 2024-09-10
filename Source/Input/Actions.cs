using LCVR.Assets;
using UnityEngine.InputSystem;

namespace LCVR.Input;

public class Actions
{
    public static Actions Instance { get; private set; } = new();

    public InputAction HeadPosition { get; private set; }
    public InputAction HeadRotation { get; private set; }
    public InputAction HeadTrackingState { get; private set; }

    public InputAction LeftHandPosition { get; private set; }
    public InputAction LeftHandRotation { get; private set; }
    public InputAction LeftHandTrackingState { get; private set; }

    public InputAction RightHandPosition { get; private set; }
    public InputAction RightHandRotation { get; private set; }
    public InputAction RightHandTrackingState { get; private set; }

    private Actions()
    {
        HeadPosition = AssetManager.DefaultXRActions.FindAction("Head/Position");
        HeadRotation = AssetManager.DefaultXRActions.FindAction("Head/Rotation");
        HeadTrackingState = AssetManager.DefaultXRActions.FindAction("Head/Tracking State");

        LeftHandPosition = AssetManager.DefaultXRActions.FindAction("Left/Position");
        LeftHandRotation = AssetManager.DefaultXRActions.FindAction("Left/Rotation");
        LeftHandTrackingState = AssetManager.DefaultXRActions.FindAction("Left/Tracking State");

        RightHandPosition = AssetManager.DefaultXRActions.FindAction("Right/Position");
        RightHandRotation = AssetManager.DefaultXRActions.FindAction("Right/Rotation");
        RightHandTrackingState = AssetManager.DefaultXRActions.FindAction("Right/Tracking State");
    }

    public InputAction this[string name] => IngamePlayerSettings.Instance.playerInput.actions[name];
}
