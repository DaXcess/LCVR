using LCVR.Input;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.HighDefinition;
using System.Collections.Generic;
using LCVR.Assets;
using UnityEngine.XR.Interaction.Toolkit;
using System.Security.Cryptography;

namespace LCVR
{
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

            driver.positionAction = Actions.Head_Position;
            driver.rotationAction = Actions.Head_Rotation;
            driver.trackingStateInput = new InputActionProperty(Actions.Head_TrackingState);
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

        public static void CreateInteractorController(this GameObject @object, string hand)
        {
            var controller = @object.AddComponent<ActionBasedController>();
            @object.AddComponent<XRRayInteractor>();
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

            renderer.material = AssetManager.defaultRayMat;

            controller.AddActionBasedControllerBinds(hand);
        }

        public static void AddActionBasedControllerBinds(this ActionBasedController controller, string hand, bool trackingEnabled = true, bool actionsEnabled = true)
        {
            controller.enableInputTracking = trackingEnabled;
            controller.positionAction = new InputActionProperty(AssetManager.defaultInputActions.FindAction($"{hand}/Position"));
            controller.rotationAction = new InputActionProperty(AssetManager.defaultInputActions.FindAction($"{hand}/Rotation"));
            controller.trackingStateAction = new InputActionProperty(AssetManager.defaultInputActions.FindAction($"{hand}/Tracking State"));

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

        public static bool BoxCast(this Ray ray, float radius, out RaycastHit hit, float maxDistance = Mathf.Infinity, int layerMask = Physics.DefaultRaycastLayers)
        {
            return Physics.BoxCast(ray.origin, Vector3.one * radius, ray.direction, out hit, Quaternion.identity, maxDistance, layerMask);
        }
    }
}
