using LCVR.Assets;
using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine.InputSystem;

namespace LCVR.Input
{
    public class Actions
    {
        private static readonly Dictionary<string, InputActionAsset> profiles = new()
        {
            { "default", AssetManager.Input("DefaultInputs") }
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

        private static InputActionAsset internalActions;
        private static readonly InputActionAsset allActions;

        static Actions()
        {
            allActions = internalActions = GetProfile("default");
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

        private static readonly Dictionary<string, InputActionAsset> cache = [];

        /// <summary>
        /// Dynamically download controller profiles from GitHub, so that users won't have to
        /// mess around with files and only need to specify a profile name inside the configuration.
        /// </summary>
        /// <returns></returns>
        private static InputActionAsset DownloadControllerProfile(string profile)
        {
            if (cache.TryGetValue(profile, out var asset)) 
                return asset;

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
                return internalActions;
            }
        }

        private static InputActionAsset GetProfile(string profile)
        {
            if (!profiles.TryGetValue(profile, out var inputAsset))
            {
                Logger.LogWarning($"Tried to load unknown controller profile: {profile}, falling back to default");
                inputAsset = profiles["default"];
            }

            internalActions = inputAsset;

            // Download external profile if configured
            var actions = string.IsNullOrEmpty(Plugin.Config.ControllerBindingsOverrideProfile.Value) switch
            {
                true => internalActions,
                false => DownloadControllerProfile(Plugin.Config.ControllerBindingsOverrideProfile.Value)
            };

            return actions;
        }

        public static void ApplyInternalControllerProfile(string profile)
        {
            var actions = GetProfile(profile);
            allActions.LoadFromJson(actions.ToJson());
            allActions.Enable();
            
            ReloadInputBindings();
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
