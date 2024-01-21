using LCVR.Assets;
using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine.InputSystem;
using UnityEngine.XR.OpenXR.Features.Interactions;

namespace LCVR.Input
{
    public class Actions
    {
        private static readonly Dictionary<string, InputActionAsset> profiles = new()
        {
            { "default", AssetManager.Input("DefaultInputs") },
            { "htc_vive", AssetManager.Input("HtcViveInputs") }
        };

        public static InputAction Head_Position;
        public static InputAction Head_Rotation;
        public static InputAction Head_TrackingState;

        public static InputAction RightHand_Position;
        public static InputAction RightHand_Rotation;
        public static InputAction RightHand_TrackingState;

        public static InputAction LeftHand_Position;
        public static InputAction LeftHand_Rotation;
        public static InputAction LeftHand_TrackingState;

        private static readonly InputActionAsset allActions;

        public static string AllActionsString => allActions.ToJson();

        static Actions()
        {
            if (!DetectControllerProfile(out var profile))
            {
                Logger.LogWarning("Failed to detect controllers (not yet connected?). Using last controller profile as fallback.");

                var lastProfile = Plugin.Config.LastInternalControllerProfile.Value;

                if (string.IsNullOrEmpty(lastProfile))
                    profile = "default";
                else
                    profile = lastProfile;
            }

            Plugin.Config.LastInternalControllerProfile.Value = profile;

            allActions = GetProfile(profile);
            allActions.Enable();

            Head_Position = allActions.FindAction("Head/Position");
            Head_Rotation = allActions.FindAction("Head/Rotation");
            Head_TrackingState = allActions.FindAction("Head/Tracking State");

            RightHand_Position = allActions.FindAction("Right Hand/Position");
            RightHand_Rotation = allActions.FindAction("Right Hand/Rotation");
            RightHand_TrackingState = allActions.FindAction("Right Hand/Tracking State");

            LeftHand_Position = allActions.FindAction("Left Hand/Position");
            LeftHand_Rotation = allActions.FindAction("Left Hand/Rotation");
            LeftHand_TrackingState = allActions.FindAction("Left Hand/Tracking State");
        }

        /// <summary>
        /// Detect the type of controllers that are being used
        /// </summary>
        private static bool DetectControllerProfile(out string profile)
        {
            profile = "";

            foreach (var device in InputSystem.devices)
            {
                if (device is OculusTouchControllerProfile.OculusTouchController || device is KHRSimpleControllerProfile.KHRSimpleController || device is MetaQuestTouchProControllerProfile.QuestProTouchController)
                {
                    // Apply default profile
                    profile = "default";
                    break;
                }
                else if (device is ValveIndexControllerProfile.ValveIndexController)
                {
                    // Apply valve index profile
                    profile = "index";
                    break;
                }
                else if (device is HTCViveControllerProfile.ViveController)
                {
                    // Apply HTC vive controller profile
                    profile = "htc_vive";
                    break;
                }
                else if (device is HPReverbG2ControllerProfile.ReverbG2Controller)
                {
                    // Apply HP Reverb G2 controller profile
                    profile = "hp_reverb";
                    break;
                }
                else if (device is MicrosoftMotionControllerProfile.WMRSpatialController)
                {
                    // Apply WMR controller profile
                    profile = "wmr";
                    break;
                }
            }

            if (string.IsNullOrEmpty(profile))
                return false;

            Logger.Log($"Detected controllers, applying controller profile '{profile}'...");

            return true;
        }

        private static readonly Dictionary<string, InputActionAsset> cache = [];

        /// <summary>
        /// Dynamically download controller profiles from GitHub, so that users won't have to
        /// mess around with files and only need to specify a profile name inside the configuration.
        /// </summary>
        /// <returns></returns>
        private static bool DownloadControllerProfile(string profile, out InputActionAsset asset)
        {
            if (cache.TryGetValue(profile, out asset)) 
                return true;

            try
            {
                if (string.IsNullOrEmpty(profile))
                    throw new Exception("Using default controller profile");

                using var client = new WebClient();

                var actions = client.DownloadString($"https://raw.githubusercontent.com/DaXcess/LCVR-Controller-Profiles/main/{profile}/profile.inputactions");
                asset = InputActionAsset.FromJson(actions);

                cache.Add(profile, asset);

                return asset;
            }
            catch
            {
                return false;
            }
        }

        private static InputActionAsset GetProfile(string profile)
        {
            if (!profiles.TryGetValue(profile, out var inputAsset))
            {
                Logger.LogWarning($"Tried to load unknown controller profile: {profile}, falling back to default");
                inputAsset = profiles["default"];
            }

            // Download external profile if configured
            var actions = string.IsNullOrEmpty(Plugin.Config.ControllerBindingsOverrideProfile.Value) switch
            {
                true => inputAsset,
                false => DownloadControllerProfile(Plugin.Config.ControllerBindingsOverrideProfile.Value, out var downloadedAsset) switch
                {
                    true => downloadedAsset,
                    false => inputAsset
                }
            };

            return actions;
        }

        public static void ReloadInputBindings()
        {
            IngamePlayerSettings.Instance.playerInput.actions = allActions;

            Logger.LogDebug("Loaded XR input binding overrides");
        }

        public static InputAction FindAction(string name)
        {
            return allActions.FindAction(name);
        }
    }
}
