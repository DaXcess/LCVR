using BepInEx.Configuration;
using System;
using UnityEngine.Rendering;

namespace LCVR;

public class Config(ConfigFile file)
{
    public ConfigFile File { get; } = file;

    // General configuration

    public ConfigEntry<bool> DisableVR { get; } = file.Bind("General", "DisableVR", false, "Disables the main functionality of this mod, can be used if you want to play without VR while keeping the mod installed.");

    public ConfigEntry<bool> AskOnStartup { get; } = file.Bind("General", "AskOnStartup", false, "When enabled, shows a popup on game launch where you are asked whether or not you want to play in VR. If DisableVR is set to true, this popup will not show.");
    public ConfigEntry<bool> IntroScreenSeen { get; } = file.Bind("General", "IntroScreenSeen", false, "Whether the VR intro screen has been displayed before. This configuration option should be set automatically.");
    public ConfigEntry<bool> EnableHelmetVisor { get; } = file.Bind("General", "EnableHelmetVisor", false, "Enables the first person helmet visor and helmet. This will restrict your field of view, but looks more immersive.");

    // Performance configuration

    public ConfigEntry<bool> EnableDynamicResolution { get; } = file.Bind("Performance", "EnableDynamicResolution", false, "Whether or not dynamic resolution should be enabled. Required for most of these settings to have an effect.");
    public ConfigEntry<DynamicResUpscaleFilter> DynamicResolutionUpscaleFilter { get; } = file.Bind("Performance", "DynamicResolutionUpscaleFilter", DynamicResUpscaleFilter.EdgeAdaptiveScalingUpres, new ConfigDescription("The filter/algorithm that will be used to perform dynamic resolution upscaling. Defaulted to FSR (Edge Adaptive Scaling).", new AcceptableValueEnum<DynamicResUpscaleFilter>()));
    public ConfigEntry<float> DynamicResolutionPercentage { get; } = file.Bind("Performance", "DynamicResolutionPercentage", 80f, new ConfigDescription("The percentage of resolution to scale the game down to. The lower the value, the harder the upscale filter has to work which will result in quality loss.", new AcceptableValueRange<float>(0, 100)));
    public ConfigEntry<bool> EnableDLSS { get; } = file.Bind("Performance", "EnableDLSS", false, "(Not recommended!) Enable DLSS support for the game. Requires dynamic resolution to be enabled. DLSS will override the upscale filter used.");
    public ConfigEntry<float> CameraResolution { get; } = file.Bind("Performance", "CameraResolution", 0.75f, new ConfigDescription("This setting configures the resolution scale of the game, lower values are more performant, but will make the game look worse. From around 0.8 the difference is negligible (on a Quest 2, with dynamic resolution disabled).", new AcceptableValueRange<float>(0.05f, 1f)));
    public ConfigEntry<bool> DisableVolumetrics { get; } = file.Bind("Performance", "DisableVolumetrics", false, "Disables volumetrics in the game, which significantly improves performance, but removes all fog and may be considered cheating.");

    // Input configuration

    public ConfigEntry<TurnProviderOption> TurnProvider = file.Bind("Input", "TurnProvider", TurnProviderOption.Snap, new ConfigDescription($"Specify which turning provider your player uses, if any.", new AcceptableValueEnum<TurnProviderOption>()));
    public ConfigEntry<float> SmoothTurnSpeedModifier { get; } = file.Bind("Input", "SmoothTurnSpeedModifier", 1f, new ConfigDescription("A multiplier that is added to the smooth turning speed. Requires turn provider to be set to smooth.", new AcceptableValueRange<float>(0.25f, 5)));
    public ConfigEntry<float> SnapTurnSize { get; } = file.Bind("Input", "SnapTurnSize", 45f, new ConfigDescription("The amount of rotation that is applied when performing a snap turn. Requires turn provider to be set to snap.", new AcceptableValueRange<float>(10, 180)));
    public ConfigEntry<bool> ToggleSprint { get; } = file.Bind("Input", "ToggleSprint", false, "Whether the sprint button should toggle sprint instead of having to hold it down.");
    public ConfigEntry<float> MovementSprintToggleCooldown { get; } = file.Bind("Input", "MovementSprintToggleCooldown", 1f, new ConfigDescription("The amount of seconds that you need to stand still for sprint to be toggled off automatically. Requires sprint toggle to be enabled.", new AcceptableValueRange<float>(0, 60)));
    public ConfigEntry<string> ControllerBindingsOverrideProfile { get; } = file.Bind("Input", "ControllerBindingsOverrideProfile", "", "Specify the name of a controler profile you would like to use. Keep empty to use the built-in controller profiles. You can find a list of available controller profiles on https://github.com/DaXcess/LCVR-Controller-Profiles. To test a local profile, specify a path using the file protocol (e.g. file:///C:/Users/.../profile.inputactions)");

    // UI configuration

    public ConfigEntry<bool> DisableArmHUD { get; } = file.Bind("UI", "DisableArmHUD", false, "Removes the HUD from the arms and displays them in front of the camera.");
    public ConfigEntry<float> HUDOffsetX { get; } = file.Bind("UI", "OffsetX", 0f, "The x offset of the HUD that was placed on the camera instead of the arms. Requires the arm HUD to be disabled.");
    public ConfigEntry<float> HUDOffsetY { get; } = file.Bind("UI", "OffsetY", 0f, "The y offset of the HUD that was placed on the camera instead of the arms. Requires the arm HUD to be disabled.");
    public ConfigEntry<bool> EnableInteractRay { get; } = file.Bind("UI", "EnableInteractRay", false, "Enable a visible ray coming out of the hand that tells you where you are currently aiming at. More or less for debug purposes, but can be used to get a feel for the rotations and such.");

    // Rendering configuration

    public ConfigEntry<bool> EnableCustomCamera { get; } = file.Bind("Rendering", "EnableCustomCamera", false, "Adds a second camera mounted on top of the VR camera that will render seperately from the VR camera to the monitor. This requires quite a bit of extra GPU power!");
    public ConfigEntry<float> CustomCameraFOV { get; } = file.Bind("Rendering", "CustomCameraFOV", 90f, new ConfigDescription("The field of view that the custom camera should have.", new AcceptableValueRange<float>(45, 120)));
    public ConfigEntry<float> CustomCameraLerpFactor { get; } = file.Bind("Rendering", "CustomCameraLerpFactor", 0.1f, new ConfigDescription("The smoothing factor of the custom camera rotation. Higher values mean more static movement, lower values are more smooth.", new AcceptableValueRange<float>(0.01f, 1f)));
    public ConfigEntry<float> LODBias { get; } = file.Bind("Rendering", "LODBias", 2f, new ConfigDescription("The LOD bias is a multiplier that dictates when an LOD must reduce their quality. Higher values means that more detailed LODs will persist for longer.", new AcceptableValueRange<float>(1, 5)));
    public ConfigEntry<bool> DisableLensDistortion { get; } = file.Bind("Rendering", "DisableLensDistortion", false, "Disables the warping effects that you experience when you are under water, use the TZP-inhalant and more.");

    // Interaction configuration

    public ConfigEntry<bool> DisableShipLeverInteraction { get; } = file.Bind("Interaction", "DisableShipLeverInteraction", false, "Disables the physical lever pull interaction on the ship lever.");
    public ConfigEntry<bool> DisableChargeStationInteraction { get; } = file.Bind("Interaction", "DisableChargeStationInteraction", false, "Disables needing to hold items up to the charger physically.");
    public ConfigEntry<bool> DisableMonitorInteraction { get; } = file.Bind("Interaction", "DisableMonitorInteraction", false, "Disables needing to physically press the buttons for the monitor.");
    public ConfigEntry<bool> DisableShipDoorInteraction { get; } = file.Bind("Interaction", "DisableShipDoorInteraction", false, "Disables needing to physically press the buttons for the ship door.");
    public ConfigEntry<bool> DisableTeleporterInteraction { get; } = file.Bind("Interaction", "DisableTeleporterInteraction", false, "Disables needing to physically press the buttons for the teleporters.");
    public ConfigEntry<bool> DisableShipHornInteraction { get; } = file.Bind("Interaction", "DisableShipHornInteraction", false, "Disables needing to physically pull the cord on the ship horn.");
    public ConfigEntry<bool> DisableCompanyBellInteraction { get; } = file.Bind("Interaction", "DisableCompanyBellInteraction", false, "Disables needing to physically press the bell at the company desk.");
    public ConfigEntry<bool> DisableBreakerBoxInteraction { get; } = file.Bind("Interaction", "DisableBreakerBoxInteraction", false, "Disabled needing to physically open the breaker box and flip the switches with your finger.");
    public ConfigEntry<bool> DisableDoorInteraction { get; } = file.Bind("Interaction", "DisableDoorInteraction", false, "Disable needing to physically open and close doors by interacting with the door handles. Will also disable the need to use keys and lockpickers physically on the door handle.");

    public ConfigEntry<bool> DisableHangarLeverInteraction { get; } = file.Bind("Interaction", "DisableHangarLeverInteraction", false, "Disable needing to physically pull the lever for the big doors on Artiface");
    
    public ConfigEntry<bool> DisableMuffleInteraction { get; } = file.Bind("Interaction", "DisableMuffleInteraction", false, "Disables the self-muffling feature, which makes it so that holding your hand in front of your mouth will no longer make you inaudible to enemies.");
    public ConfigEntry<bool> DisableFaceInteractions { get; } = file.Bind("Interaction", "DisableFaceInteractions", false, "Disables the functionality to hold certain items up to your face to use them.");

    // Internal configuration

    public ConfigEntry<string> LastInternalControllerProfile { get; } = file.Bind("Internal", "LastInternalControllerProfile", "", "FOR INTERNAL USE ONLY, DO NOT EDIT");
    public ConfigEntry<string> OpenXRRuntimeFile { get; } = file.Bind("Internal", "OpenXRRuntimeFile", "", "FOR INTERNAL USE ONLY, DO NOT EDIT");
    public ConfigEntry<bool> DisableSettingsButton { get; } = file.Bind("Internal", "DisableSettingsButton", false,
        "Disables the settings button on the main menu screen");
    
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
