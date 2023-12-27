using LCVR.Input;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.HighDefinition;

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
        public static void DisableQualitySetting(HDAdditionalCameraData camera, FrameSettingsField setting)
        {
            camera.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)setting] = true;
            camera.renderingPathCustomFrameSettings.SetEnabled(setting, false);
        }

        public static void AttachHeadTrackedPoseDriver(this GameObject @object)
        {
            var driver = @object.AddComponent<TrackedPoseDriver>();

            driver.positionAction = Actions.XR_HeadPosition;
            driver.rotationAction = Actions.XR_HeadRotation;
            driver.trackingStateInput = new InputActionProperty(Actions.XR_HeadTrackingState);
        }
    }
}
