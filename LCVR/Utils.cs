using LCVR.Input;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.HighDefinition;
using System.Collections.Generic;
using LCVR.Assets;
using UnityEngine.XR.Interaction.Toolkit;
using System.Security.Cryptography;
using System.Text;
using System;
using GameNetcodeStuff;

namespace LCVR;

internal static class GameObjectExtensions
{
    public static GameObject Find(this GameObject @object, string name)
    {
        return @object.transform.Find(name)?.gameObject;
    }
}

internal static class Utils
{
    public static byte[] ComputeHash(byte[] input)
    {
        using var sha = SHA256.Create();

        return sha.ComputeHash(input);
    }

    public static string GetPath(this Transform current)
    {
        if (current.parent == null)
            return "/" + current.name;
        return current.parent.GetPath() + "/" + current.name;
    }

    public static string FormatPascalAndAcronym(string input)
    {
        var builder = new StringBuilder(input[0].ToString());
        if (builder.Length > 0)
        {
            for (var index = 1; index < input.Length; index++)
            {
                char prevChar = input[index - 1];
                char nextChar = index + 1 < input.Length ? input[index + 1] : '\0';

                bool isNextLower = char.IsLower(nextChar);
                bool isNextUpper = char.IsUpper(nextChar);
                bool isPresentUpper = char.IsUpper(input[index]);
                bool isPrevLower = char.IsLower(prevChar);
                bool isPrevUpper = char.IsUpper(prevChar);

                if (!string.IsNullOrWhiteSpace(prevChar.ToString()) &&
                    ((isPrevUpper && isPresentUpper && isNextLower) ||
                    (isPrevLower && isPresentUpper && isNextLower) ||
                    (isPrevLower && isPresentUpper && isNextUpper)))
                {
                    builder.Append(' ');
                    builder.Append(input[index]);
                }
                else
                {
                    builder.Append(input[index]);
                }
            }
        }
        return builder.ToString();
    }

    public static void EnableQualitySetting(this HDAdditionalCameraData camera, FrameSettingsField setting)
    {
        camera.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)setting] = false;
        camera.renderingPathCustomFrameSettings.SetEnabled(setting, true);
    }

    public static void DisableQualitySetting(this HDAdditionalCameraData camera, FrameSettingsField setting)
    {
        camera.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)setting] = true;
        camera.renderingPathCustomFrameSettings.SetEnabled(setting, false);
    }

    public static void AttachHeadTrackedPoseDriver(this GameObject @object)
    {
        var driver = @object.AddComponent<TrackedPoseDriver>();

        driver.positionAction = Actions.Instance.HeadPosition;
        driver.rotationAction = Actions.Instance.HeadRotation;
        driver.trackingStateInput = new InputActionProperty(Actions.Instance.HeadTrackingState);
    }

    public static Transform[] GetChildren(this Transform transform)
    {
        var children = new List<Transform>();

        for (var i = 0; i < transform.childCount; i++)
            children.Add(transform.GetChild(i));

        return children.ToArray();
    }

    public static void ApplyOffsetTransform(this Transform transform, Transform parent, Vector3 positionOffset, Vector3 rotationOffset)
    {
        transform.rotation = parent.rotation;
        transform.Rotate(rotationOffset);
        transform.position = parent.position + parent.rotation * positionOffset;
    }

    public static bool IsInactivePlayer(this PlayerControllerB player)
    {
        if (player == StartOfRound.Instance.localPlayerController)
            return false;

        return !player.transform.Find("ScavengerModel/metarig/CameraContainer/MainCamera").GetComponent<Camera>().enabled;
    }

    public static XRRayInteractor CreateInteractorController(this GameObject @object, Hand hand, bool rayVisible = true, bool trackingEnabled = true, bool actionsEnabled = true)
    {
        var controller = @object.AddComponent<ActionBasedController>();
        var interactor = @object.AddComponent<XRRayInteractor>();
        var visual = @object.AddComponent<XRInteractorLineVisual>();
        var renderer = @object.GetComponent<LineRenderer>();

        visual.lineBendRatio = 1;
        visual.invalidColorGradient = new Gradient()
        {
            mode = GradientMode.Blend,
            alphaKeys = [
                new GradientAlphaKey(0.1f, 0),
                new GradientAlphaKey(0.1f, 1)
            ],
            colorKeys = [
                new GradientColorKey(Color.white, 0),
                new GradientColorKey(Color.white, 1)
            ]
        };
        visual.enabled = rayVisible;

        renderer.material = AssetManager.defaultRayMat;

        controller.AddActionBasedControllerBinds(hand, trackingEnabled, actionsEnabled);

        return interactor;
    }

    public static void AddActionBasedControllerBinds(this ActionBasedController controller, Hand hand, bool trackingEnabled = true, bool actionsEnabled = true)
    {
        controller.enableInputTracking = trackingEnabled;
        controller.positionAction = new InputActionProperty(hand.Position());
        controller.rotationAction = new InputActionProperty(hand.Rotation());
        controller.trackingStateAction = new InputActionProperty(hand.TrackingState());

        controller.enableInputActions = actionsEnabled;
        controller.selectAction = new InputActionProperty(AssetManager.defaultInputActions.FindAction($"{hand}/Select"));
        controller.selectActionValue = new InputActionProperty(AssetManager.defaultInputActions.FindAction($"{hand}/Select Value"));
        controller.activateAction = new InputActionProperty(AssetManager.defaultInputActions.FindAction($"{hand}/Activate"));
        controller.activateActionValue = new InputActionProperty(AssetManager.defaultInputActions.FindAction($"{hand}/Activate Value"));
        controller.uiPressAction = new InputActionProperty(AssetManager.defaultInputActions.FindAction($"{hand}/UI Press"));
        controller.uiPressActionValue = new InputActionProperty(AssetManager.defaultInputActions.FindAction($"{hand}/UI Press Value"));
        controller.uiScrollAction = new InputActionProperty(AssetManager.defaultInputActions.FindAction($"{hand}/UI Scroll"));
        controller.rotateAnchorAction = new InputActionProperty(AssetManager.defaultInputActions.FindAction($"{hand}/Rotate Anchor"));
        controller.translateAnchorAction = new InputActionProperty(AssetManager.defaultInputActions.FindAction($"{hand}/Translate Anchor"));
        controller.scaleToggleAction = new InputActionProperty(AssetManager.defaultInputActions.FindAction($"{hand}/Scale Toggle"));
        controller.scaleDeltaAction = new InputActionProperty(AssetManager.defaultInputActions.FindAction($"{hand}/Scale Delta"));
    }

    public static bool Raycast(this Ray ray, out RaycastHit hit, float maxDistance = Mathf.Infinity, int layerMask = UnityEngine.Physics.DefaultRaycastLayers)
    {
        return UnityEngine.Physics.Raycast(ray, out hit, maxDistance, layerMask);
    }

    public static bool BoxCast(this Ray ray, float radius, out RaycastHit hit, float maxDistance = Mathf.Infinity, int layerMask = UnityEngine.Physics.DefaultRaycastLayers)
    {
        return UnityEngine.Physics.BoxCast(ray.origin, Vector3.one * radius, ray.direction, out hit, Quaternion.identity, maxDistance, layerMask);
    }

    public enum Hand
    {
        Left,
        Right,
    }

    private static InputAction Position(this Hand hand)
    {
        return hand switch
        {
            Hand.Left => Actions.Instance.LeftHandPosition,
            Hand.Right => Actions.Instance.RightHandPosition,
            _ => throw new NotImplementedException(),
        };
    }

    private static InputAction Rotation(this Hand hand)
    {
        return hand switch
        {
            Hand.Left => Actions.Instance.LeftHandRotation,
            Hand.Right => Actions.Instance.RightHandRotation,
            _ => throw new NotImplementedException(),
        };
    }

    private static InputAction TrackingState(this Hand hand)
    {
        return hand switch
        {
            Hand.Left => Actions.Instance.LeftHandTrackingState,
            Hand.Right => Actions.Instance.RightHandTrackingState,
            _ => throw new NotImplementedException(),
        };
    }
}
