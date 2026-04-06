using LCVR.Assets;
using UnityEngine.InputSystem;

namespace LCVR.Input;

public class Actions
{
    public static Actions Instance { get; private set; } = new();

    public readonly InputAction HeadPosition;
    public readonly InputAction HeadRotation;
    public readonly InputAction HeadTrackingState;

    public readonly InputAction LeftHandPosition;
    public readonly InputAction LeftHandRotation;
    public readonly InputAction LeftHandVelocity;
    public readonly InputAction LeftHandTrackingState;

    public readonly InputAction RightHandPosition;
    public readonly InputAction RightHandRotation;
    public readonly InputAction RightHandVelocity;
    public readonly InputAction RightHandTrackingState;

    private Actions()
    {
        HeadPosition = AssetManager.DefaultXRActions.FindAction("Head/Position");
        HeadRotation = AssetManager.DefaultXRActions.FindAction("Head/Rotation");
        HeadTrackingState = AssetManager.DefaultXRActions.FindAction("Head/Tracking State");

        LeftHandPosition = AssetManager.DefaultXRActions.FindAction("Left/Position");
        LeftHandRotation = AssetManager.DefaultXRActions.FindAction("Left/Rotation");
        LeftHandVelocity = AssetManager.DefaultXRActions.FindAction("Left/Velocity");
        LeftHandTrackingState = AssetManager.DefaultXRActions.FindAction("Left/Tracking State");

        RightHandPosition = AssetManager.DefaultXRActions.FindAction("Right/Position");
        RightHandRotation = AssetManager.DefaultXRActions.FindAction("Right/Rotation");
        RightHandVelocity = AssetManager.DefaultXRActions.FindAction("Right/Velocity");
        RightHandTrackingState = AssetManager.DefaultXRActions.FindAction("Right/Tracking State");

        AssetManager.DefaultXRActions.Enable();
    }

    public InputAction this[string name] => IngamePlayerSettings.Instance.playerInput.actions[name];
}
