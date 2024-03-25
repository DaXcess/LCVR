using LCVR.Assets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEngine.InputSystem;
using UnityEngine.XR.OpenXR.Features.Interactions;

namespace LCVR.Input;

public class Actions
{
    private static readonly Dictionary<string, InputActionAsset> cache = [];
    private static readonly Dictionary<string, InputActionAsset> profiles = new()
    {
        { "default", AssetManager.Input("DefaultInputs") },
        { "htc_vive", AssetManager.Input("HtcViveInputs") },
        { "index", AssetManager.Input("ValveIndexInputs") },
        { "wmr", AssetManager.Input("WmrInputs") },
        { "hp_reverb", AssetManager.Input("HPReverbInputs") },
    };

    public static Actions Instance { get; private set; } = new();

    private InputActionAsset allActions;

    public event Action<InputActionAsset, InputActionAsset> OnReload;

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
        Reload();

        HeadPosition = AssetManager.defaultInputActions.FindAction("Head/Position");
        HeadRotation = AssetManager.defaultInputActions.FindAction("Head/Rotation");
        HeadTrackingState = AssetManager.defaultInputActions.FindAction("Head/Tracking State");

        LeftHandPosition = AssetManager.defaultInputActions.FindAction("Left/Position");
        LeftHandRotation = AssetManager.defaultInputActions.FindAction("Left/Rotation");
        LeftHandTrackingState = AssetManager.defaultInputActions.FindAction("Left/Tracking State");

        RightHandPosition = AssetManager.defaultInputActions.FindAction("Right/Position");
        RightHandRotation = AssetManager.defaultInputActions.FindAction("Right/Rotation");
        RightHandTrackingState = AssetManager.defaultInputActions.FindAction("Right/Tracking State");
    }

    public void Reload()
    {
        if (!DetectControllerProfile(out var profile))
        {
            InputSystem.onDeviceChange += InputSystem_onDeviceChange;

            if (string.IsNullOrEmpty(Plugin.Config.LastInternalControllerProfile.Value))
                Plugin.Config.LastInternalControllerProfile.Value = "default";

            profile = Plugin.Config.LastInternalControllerProfile.Value;
        }

        var oldActions = allActions;

        allActions = GetProfile(profile);
        allActions.Enable();

        IngamePlayerSettings.Instance.playerInput.actions = allActions;
        OnReload?.Invoke(oldActions, allActions);
        
        Logger.LogDebug("Loaded XR input binding overrides");
    }

    private void InputSystem_onDeviceChange(InputDevice _1, InputDeviceChange _2)
    {
        if (!DetectControllerProfile(out var profile))
            return;

        InputSystem.onDeviceChange -= InputSystem_onDeviceChange;

        Plugin.Config.LastInternalControllerProfile.Value = profile;

        allActions = GetProfile(profile);
        allActions.Enable();
    }

    /// <summary>
    /// Detect the type of controllers that are being used
    /// </summary>
    private bool DetectControllerProfile(out string profile)
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

    /// <summary>
    /// Dynamically download controller profiles from GitHub, so that users won't have to
    /// mess around with files and only need to specify a profile name inside the configuration.
    /// 
    /// To use a local profile file, use a file:/// path as the profile name, that points to the
    /// location of the local file.
    /// </summary>
    /// <returns></returns>
    private bool DownloadControllerProfile(string profile, out InputActionAsset asset)
    {
        if (cache.TryGetValue(profile, out asset))
            return true;

        try
        {
            if (string.IsNullOrEmpty(profile))
                throw new Exception("Using default controller profile");

            if (Uri.TryCreate(profile, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeFile)
            {
                var path = Uri.UnescapeDataString(uri.LocalPath);
                var data = File.ReadAllText(path);
                asset = InputActionAsset.FromJson(data);

                cache.Add(profile, asset);

                return true;
            }

            using var client = new WebClient();

            var actions = client.DownloadString($"https://raw.githubusercontent.com/DaXcess/LCVR-Controller-Profiles/main/{profile}/profile.inputactions");
            asset = InputActionAsset.FromJson(actions);

            cache.Add(profile, asset);

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"Failed to load override binding profile: {ex.Message}");
            return false;
        }
    }

    private InputActionAsset GetProfile(string profile)
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

    public InputAction this[string name] => allActions.FindAction(name);
}
