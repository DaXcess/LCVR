using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine.Rendering;

namespace LCVR;

public class Config(string assemblyPath, ConfigFile file)
{
    private const string PERSISTENT_KEY = "io.daxcess.lcvr.persistent-settings";

    public string AssemblyPath { get; } = assemblyPath;
    public ConfigFile File { get; } = file;

    private bool isSerializing;

    // General configuration

    public ConfigEntry<bool> DisableVR { get; } = file.Bind("General", "DisableVR", false,
        "Disables the main functionality of this mod, can be used if you want to play without VR while keeping the mod installed.");

    public ConfigEntry<bool> AskOnStartup { get; } = file.Bind("General", "AskOnStartup", false,
        "When enabled, shows a popup on game launch where you are asked whether or not you want to play in VR. If DisableVR is set to true, this popup will not show.");

    public ConfigEntry<bool> EnableHelmetVisor { get; } = file.Bind("General", "EnableHelmetVisor", false,
        "Enables the first person helmet visor and helmet. This will restrict your field of view, but looks more immersive.");

    public ConfigEntry<bool> EnableVerboseLogging { get; } = file.Bind("General", "EnableVerboseLogging", false,
        "Enables verbose debug logging during OpenXR initialization");

    [ES3NonSerializable]
    public ConfigEntry<bool> DisablePersistentSettings { get; } =
        file.Bind("General", "DisablePersistentSettings", false,
            "Persistent settings makes sure that all of your LCVR settings are the same across different modpacks. Disabling this will mean that your LCVR settings will be reset every time you create a new modpack.");

    // Performance configuration

    public ConfigEntry<bool> EnableOcclusionMesh { get; } = file.Bind("Performance", "EnableOcclusionMesh", true,
        "The occlusion mesh will cause the game to stop rendering pixels outside of the lens views, which increases performance.");

    public ConfigEntry<bool> EnableDynamicResolution { get; } = file.Bind("Performance", "EnableDynamicResolution",
        false,
        "Whether or not dynamic resolution should be enabled. Required for most of these settings to have an effect.");

    public ConfigEntry<DynamicResUpscaleFilter> DynamicResolutionUpscaleFilter { get; } = file.Bind("Performance",
        "DynamicResolutionUpscaleFilter", DynamicResUpscaleFilter.EdgeAdaptiveScalingUpres,
        new ConfigDescription(
            "The filter/algorithm that will be used to perform dynamic resolution upscaling. Defaulted to FSR (Edge Adaptive Scaling).",
            new AcceptableValueEnum<DynamicResUpscaleFilter>()));

    public ConfigEntry<float> DynamicResolutionPercentage { get; } = file.Bind("Performance",
        "DynamicResolutionPercentage", 80f,
        new ConfigDescription(
            "The percentage of resolution to scale the game down to. The lower the value, the harder the upscale filter has to work which will result in quality loss.",
            new AcceptableValueRange<float>(10, 100)));

    public ConfigEntry<float> CameraResolution { get; } = file.Bind("Performance", "CameraResolution", 0.75f,
        new ConfigDescription(
            "This setting configures the resolution scale of the game, lower values are more performant, but will make the game look worse. From around 0.8 the difference is negligible (on a Quest 2, with dynamic resolution disabled).",
            new AcceptableValueRange<float>(0.05f, 1.5f)));

    public ConfigEntry<bool> DisableVolumetrics { get; } = file.Bind("Performance", "DisableVolumetrics", false,
        "Disables volumetrics in the game, which significantly improves performance, but removes all fog and may be considered cheating.");

    // Input configuration

    public ConfigEntry<TurnProviderOption> TurnProvider { get; } = file.Bind("Input", "TurnProvider",
        TurnProviderOption.Snap,
        new ConfigDescription($"Specify which turning provider your player uses, if any.",
            new AcceptableValueEnum<TurnProviderOption>()));

    public ConfigEntry<float> SmoothTurnSpeedModifier { get; } = file.Bind("Input", "SmoothTurnSpeedModifier", 1f,
        new ConfigDescription(
            "A multiplier that is added to the smooth turning speed. Requires turn provider to be set to smooth.",
            new AcceptableValueRange<float>(0.25f, 5)));

    public ConfigEntry<float> SnapTurnSize { get; } = file.Bind("Input", "SnapTurnSize", 45f,
        new ConfigDescription(
            "The amount of rotation that is applied when performing a snap turn. Requires turn provider to be set to snap.",
            new AcceptableValueRange<float>(10, 180)));

    public ConfigEntry<bool> ToggleSprint { get; } = file.Bind("Input", "ToggleSprint", false,
        "Whether the sprint button should toggle sprint instead of having to hold it down.");

    public ConfigEntry<float> MovementSprintToggleCooldown { get; } = file.Bind("Input", "MovementSprintToggleCooldown",
        1f,
        new ConfigDescription(
            "The amount of seconds that you need to stand still for sprint to be toggled off automatically. Requires sprint toggle to be enabled.",
            new AcceptableValueRange<float>(0, 60)));

    public ConfigEntry<float> ButtonPressPoint { get; } = file.Bind("Input", "ButtonPressPoint", 0.25f,
        new ConfigDescription(
            "The amount of force required to register a UI button press. The lower the value, the more sensitive UI presses become.",
            new AcceptableValueRange<float>(0, 1)));

    // UI configuration

    public ConfigEntry<bool> EnablePitchLockedCanvas { get; } = file.Bind("UI", "EnablePitchLockedCanvas", true,
        "Whether most of the camera-locked UI elements should only (smoothly) rotate on the Y axis, instead of being stuck on your face.");

    public ConfigEntry<bool> DisableArmHUD { get; } = file.Bind("UI", "DisableArmHUD", false,
        "Removes the HUD from the arms and displays them in front of the camera.");

    public ConfigEntry<float> HUDOffsetX { get; } = file.Bind("UI", "HUDOffsetX", 0f,
        "The x offset of the HUD that was placed on the camera instead of the arms. Requires the arm HUD to be disabled.");

    public ConfigEntry<float> HUDOffsetY { get; } = file.Bind("UI", "HUDOffsetY", 0f,
        "The y offset of the HUD that was placed on the camera instead of the arms. Requires the arm HUD to be disabled.");

    public ConfigEntry<bool> EnableInteractRay { get; } = file.Bind("UI", "EnableInteractRay", false,
        "Enable a visible ray coming out of the hand that tells you where you are currently aiming at. More or less for debug purposes, but can be used to get a feel for the rotations and such.");

    // Rendering configuration

    public ConfigEntry<bool> DisableLensDistortion { get; } = file.Bind("Rendering", "DisableLensDistortion", false,
        "Disables the warping effects that you experience when you are under water, use the TZP-inhalant and more.");

    public ConfigEntry<bool> DisableCameraShake { get; } = file.Bind("Rendering", nameof(DisableCameraShake), false,
        "Disables the camera shake that happens during explosions, rerouting, and other types of events.");

    public ConfigEntry<bool> EnableCustomCamera { get; } = file.Bind("Rendering", "EnableCustomCamera", false,
        "Adds a second camera mounted on top of the VR camera that will render seperately from the VR camera to the monitor. This requires quite a bit of extra GPU power!");

    public ConfigEntry<float> CustomCameraFOV { get; } = file.Bind("Rendering", "CustomCameraFOV", 90f,
        new ConfigDescription("The field of view that the custom camera should have.",
            new AcceptableValueRange<float>(45, 120)));

    public ConfigEntry<float> CustomCameraLerpFactor { get; } = file.Bind("Rendering", "CustomCameraLerpFactor", 0.1f,
        new ConfigDescription(
            "The smoothing factor of the custom camera rotation. Higher values mean more static movement, lower values are more smooth.",
            new AcceptableValueRange<float>(0.01f, 1f)));

    public ConfigEntry<float> LODBias { get; } = file.Bind("Rendering", "LODBias", 2f,
        new ConfigDescription(
            "The LOD bias is a multiplier that dictates when an LOD must reduce their quality. Higher values means that more detailed LODs will persist for longer.",
            new AcceptableValueRange<float>(1, 5)));

    public ConfigEntry<bool> SpectatorLightRemovesVolumetrics { get; } = file.Bind("Rendering",
        "SpectatorLightRemovesVolumetrics", false,
        "When spectating, also disable all volumetrics (fog) while the fullbright lighting is enabled for more visibility.");

    public ConfigEntry<float> MirrorXOffset { get; } = file.Bind("Rendering", "MirrorXOffset", 0f,
        new ConfigDescription(
            "The X offset that is added to the XR Mirror View shader. Do not touch if you don't know what this means.",
            new AcceptableValueRange<float>(-1, 1)));

    public ConfigEntry<float> MirrorYOffset { get; } = file.Bind("Rendering", "MirrorYOffset", 0f,
        new ConfigDescription(
            "The Y offset that is added to the XR Mirror View shader. Do not touch if you don't know what this means.",
            new AcceptableValueRange<float>(-1, 1)));

    // Interaction configuration

    public ConfigEntry<bool> DisableShipLeverInteraction { get; } = file.Bind("Interaction",
        "DisableShipLeverInteraction", false, "Disables the physical lever pull interaction on the ship lever.");

    public ConfigEntry<bool> DisableChargeStationInteraction { get; } = file.Bind("Interaction",
        "DisableChargeStationInteraction", false, "Disables needing to hold items up to the charger physically.");

    public ConfigEntry<bool> DisableMonitorInteraction { get; } = file.Bind("Interaction", "DisableMonitorInteraction",
        false, "Disables needing to physically press the buttons for the monitor.");

    public ConfigEntry<bool> DisableShipDoorInteraction { get; } = file.Bind("Interaction",
        "DisableShipDoorInteraction", false, "Disables needing to physically press the buttons for the ship door.");

    public ConfigEntry<bool> DisableTeleporterInteraction { get; } = file.Bind("Interaction",
        "DisableTeleporterInteraction", false, "Disables needing to physically press the buttons for the teleporters.");

    public ConfigEntry<bool> DisableShipHornInteraction { get; } = file.Bind("Interaction",
        "DisableShipHornInteraction", false, "Disables needing to physically pull the cord on the ship horn.");

    public ConfigEntry<bool> DisableCompanyBellInteraction { get; } = file.Bind("Interaction",
        "DisableCompanyBellInteraction", false, "Disables needing to physically press the bell at the company desk.");

    public ConfigEntry<bool> DisableBreakerBoxInteraction { get; } = file.Bind("Interaction",
        "DisableBreakerBoxInteraction", false,
        "Disable needing to physically open the breaker box and flip the switches with your finger.");

    public ConfigEntry<bool> DisableLightSwitchInteraction { get; } = file.Bind("Interaction",
        nameof(DisableLightSwitchInteraction), false, "Disable needing to physically switch light switches");

    public ConfigEntry<bool> DisableDoorInteraction { get; } = file.Bind("Interaction", "DisableDoorInteraction", false,
        "Disable needing to physically open and close doors by interacting with the door handles. Will also disable the need to use keys and lockpickers physically on the door handle.");

    public ConfigEntry<bool> DisableHangarLeverInteraction { get; } = file.Bind("Interaction",
        "DisableHangarLeverInteraction", false,
        "Disable needing to physically pull the lever for the big doors on Artiface");

    public ConfigEntry<bool> DisableMuffleInteraction { get; } = file.Bind("Interaction", "DisableMuffleInteraction",
        false,
        "Disables the self-muffling feature, which makes it so that holding your hand in front of your mouth will no longer make you inaudible to enemies.");

    public ConfigEntry<bool> DisableFaceInteractions { get; } = file.Bind("Interaction", "DisableFaceInteractions",
        false, "Disables the functionality to hold certain items up to your face to use them.");

    public ConfigEntry<bool> DisableElevatorButtonInteraction { get; } = file.Bind("Interaction",
        "DisableElevatorButtonInteraction", false, "Disables needing to physically press the elevator buttons");

    // Car interaction configuration

    public ConfigEntry<bool> DisableCarSteeringWheelInteraction { get; } = file.Bind("Car",
        "DisableCarSteeringWheelInteraction", false, "Disables the need to physically steer the Company Cruiser");

    public ConfigEntry<bool> DisableCarButtonInteractions { get; } = file.Bind("Car", "DisableCarButtonInteractions",
        false,
        "Disables the need to physically press the generic buttons in the Company Cruiser (radio, windshield wipers, etc)");

    public ConfigEntry<bool> DisableCarHonkInteraction { get; } = file.Bind("Car", "DisableCarHonkInteraction", false,
        "Disables the need to physically press the car honk in the Company Cruiser");

    public ConfigEntry<bool> DisableCarEjectInteraction { get; } = file.Bind("Car", "DisableCarEjectInteraction", false,
        "Disables the need to physically press the eject button in the Company Cruiser");

    public ConfigEntry<bool> DisableCarGearStickInteractions { get; } = file.Bind("Car",
        "DisableCarGearStickInteractions", false,
        "Disables the need to physically shift the gears in the Company Cruiser");

    public ConfigEntry<bool> DisableCarIgnitionInteractions { get; } = file.Bind("Car",
        "DisableCarIgnitionInteractions", false,
        "Disables the need to physically start/stop the car ignition in the Company Cruiser");

    // Internal configuration

    public ConfigEntry<bool> IntroScreenSeen { get; } = file.Bind("Internal", "IntroScreenSeen", false,
        "Whether the VR intro screen has been displayed before. This configuration option should be set automatically.");

    public ConfigEntry<string> ControllerBindingsOverride { get; } = file.Bind("Internal", "ControllerBindingsOverride",
        "", "FOR INTERNAL USE ONLY, DO NOT EDIT");

    public ConfigEntry<string> OpenXRRuntimeFile { get; } =
        file.Bind("Internal", "OpenXRRuntimeFile", "", "FOR INTERNAL USE ONLY, DO NOT EDIT");

    public ConfigEntry<bool> DisableSettingsButton { get; } = file.Bind("Internal", "DisableSettingsButton", false,
        "Disables the settings button on the main menu screen");

    public void SerializeToES3()
    {
        if (isSerializing || DisablePersistentSettings.Value)
            return;

        isSerializing = true;

        try
        {
            var structured = new Dictionary<string, Dictionary<string, object>>();

            foreach (var entry in File)
            {
                var section = entry.Key.Section;
                var key = entry.Key.Key;
                var value = entry.Value.BoxedValue;

                // Skip serializing if property is marked as non-serializable
                if (AccessTools.Property(typeof(Config), key) is { } prop &&
                    prop.GetCustomAttribute<ES3NonSerializable>() != null)
                    continue;

                if (!structured.ContainsKey(section))
                    structured[section] = new Dictionary<string, object>();

                structured[section][key] = value;
            }

            var json = JsonConvert.SerializeObject(structured, Formatting.None);

            ES3.Save(PERSISTENT_KEY, json);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to serialize LCVR settings to persistent storage: {ex.Message}");
        }
        finally
        {
            isSerializing = false;
        }
    }

    public void DeserializeFromES3()
    {
        if (isSerializing || DisablePersistentSettings.Value)
            return;

        isSerializing = true;

        try
        {
            if (!ES3.KeyExists(PERSISTENT_KEY))
                return;

            var json = ES3.Load<string>(PERSISTENT_KEY);
            var structured = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(json);

            foreach (var (section, settings) in structured)
            foreach (var (key, value) in settings)
            {
                var definition = new ConfigDefinition(section, key);

                if (!File.ContainsKey(definition))
                {
                    Logger.LogWarning(
                        $"Setting '{section}.{key}' is unrecognized, and will be discarded next time the settings are written to disk");
                    continue;
                }

                // Skip deserializing if property is marked as non-serializable
                if (AccessTools.Property(typeof(Config), key) is { } prop &&
                    prop.GetCustomAttribute<ES3NonSerializable>() != null)
                    continue;
                
                var entry = File[section, key];

                entry.BoxedValue = entry.SettingType.IsEnum
                    ? Enum.ToObject(entry.SettingType, value)
                    : Convert.ChangeType(value, entry.SettingType);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to deserialize LCVR settings from persistent storage: {ex.Message}");
        }
        finally
        {
            isSerializing = false;
        }
    }

    public enum TurnProviderOption
    {
        Snap,
        Smooth,
        Disabled
    }
}

internal class AcceptableValueEnum<T>() : AcceptableValueBase(typeof(T))
    where T : notnull, Enum
{
    private readonly string[] names = Enum.GetNames(typeof(T));

    public override object Clamp(object value) => value;
    public override bool IsValid(object value) => true;
    public override string ToDescriptionString() => $"# Acceptable values: {string.Join(", ", names)}";
}
