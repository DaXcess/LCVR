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
        HeadPosition = AssetManager.TrackingActions.FindAction("Head/Position");
        HeadRotation = AssetManager.TrackingActions.FindAction("Head/Rotation");
        HeadTrackingState = AssetManager.TrackingActions.FindAction("Head/Tracking State");

        LeftHandPosition = AssetManager.TrackingActions.FindAction("Left/Position");
        LeftHandRotation = AssetManager.TrackingActions.FindAction("Left/Rotation");
        LeftHandTrackingState = AssetManager.TrackingActions.FindAction("Left/Tracking State");

        RightHandPosition = AssetManager.TrackingActions.FindAction("Right/Position");
        RightHandRotation = AssetManager.TrackingActions.FindAction("Right/Rotation");
        RightHandTrackingState = AssetManager.TrackingActions.FindAction("Right/Tracking State");
    }

    public InputAction this[string name] => IngamePlayerSettings.Instance.playerInput.actions[name];
}
