using HarmonyLib;
using LethalCompanyVR.Input;
using System;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace LethalCompanyVR.UI.Patches
{
    [HarmonyPatch]
    public class UIPatches
    {
        public static Camera UICamera;

        // TODO: Remove after mod is done
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PreInitSceneScript), "Start")]
        private static void ImmediateOnline()
        {
            SceneManager.LoadScene("InitScene");
        }

        // TODO: Clean up, especially the AttachedUI part in here
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MenuManager), "Start")]
        private static void MoveToWorldSpace(MenuManager __instance)
        {
            try
            {
                var newCamera = GameObject.Find("UICamera").GetComponent<Camera>();

                if (newCamera != UICamera)
                {
                    UICamera = newCamera;

                    var driver = UICamera.gameObject.AddComponent<TrackedPoseDriver>();

                    driver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
                    driver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;

                    driver.positionAction = Actions.XR_HeadPosition;
                    driver.rotationAction = Actions.XR_HeadRotation;
                    driver.trackingStateInput = new InputActionProperty(Actions.XR_HeadTrackingState);
                }

                var canvas = __instance.GetComponentInParent<Canvas>();

                if (!canvas) return;
                if (!UICamera)
                {
                    Debug.LogError("Where is UICamera??");
                    return;
                }

                var forward = UIPatches.UICamera.transform.forward;
                forward.y = 0;
                forward.Normalize();

                var newPosition = UIPatches.UICamera.transform.position + forward * 5;
                newPosition.y = 0;

                var instance = AttachedUI.Create(canvas, 0.0085f);
                instance.SetTargetTransform(newPosition, Quaternion.Euler(0, UIPatches.UICamera.transform.rotation.eulerAngles.y, 0));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to move canvas to world space ({__instance.name}): {exception}");
            }
        }
    }
}
