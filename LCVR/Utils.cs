using LCVR.Input;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.HighDefinition;
using System.Collections.Generic;

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
    }
}
