using BepInEx.Configuration;
using System;
using UnityEngine.Rendering;

namespace LCVR
{
    public class Config(ConfigFile file)
    {
        // General configuration

        public ConfigEntry<bool> DisableVR { get; } = file.Bind("General", "DisableVR", false, "Disables the main functionality of this mod, can be used if you want to play without VR while keeping the mod installed.");
        public ConfigEntry<bool> IntroScreenSeen { get; } = file.Bind("General", "IntroScreenSeen", false, "Whether the VR intro screen has been displayed before. This configuration option should be set automatically.");
        public ConfigEntry<bool> OverrideCompatibilityVersionCheck { get; } = file.Bind("General", "OverrideCompatibilityVersionCheck", false, "If enabled, ignores the version check when detecting compatible mods. Warning: Enabling this may cause problems if non-standard compatible mod versions are used.");

        // Performance configuration

        public ConfigEntry<bool> EnableDynamicResolution { get; } = file.Bind("Performance", "EnableDynamicResolution", false, "Whether or not dynamic resolution should be enabled. Required for most of these settings to have an effect.");
        public ConfigEntry<DynamicResUpscaleFilter> DynamicResolutionUpscaleFilter { get; } = file.Bind("Performance", "DynamicResolutionUpscaleFilter", DynamicResUpscaleFilter.EdgeAdaptiveScalingUpres, new ConfigDescription("The filter/algorithm that will be used to perform dynamic resolution upscaling. Defaulted to FSR (Edge Adaptive Scaling).", new AcceptableValueEnum<DynamicResUpscaleFilter>()));
        public ConfigEntry<float> DynamicResolutionPercentage { get; } = file.Bind("Performance", "DynamicResolutionPercentage", 80f, new ConfigDescription("The percentage of resolution to scale the game down to. The lower the value, the harder the upscale filter has to work which will result in quality loss.", new AcceptableValueRange<float>(0, 100))); 
        public ConfigEntry<bool> EnableDLSS { get; } = file.Bind("Performance", "EnableDLSS", false, "Enable DLSS support for the game. Required dynamic resolution to be enabled. DLSS will override the upscale filter used.");
        public ConfigEntry<float> CameraResolution { get; } = file.Bind("Performance", "CameraResolution", 0.75f, new ConfigDescription("This setting configures the resolution scale of the game, lower values are more performant, but will make the game look worse. From around 0.8 the difference is negligible (on a Quest 2, with dynamic resolution disabled).", new AcceptableValueRange<float>(0.05f, 1f)));
        public ConfigEntry<bool> CameraResolutionGlobal { get; } = file.Bind("Performance", "CameraResolutionGlobal", false, "Whether the camera resolution scale applies to all cameras. If disabled, it is only applied on the in-game FPV camera.");
        public ConfigEntry<bool> DisableVolumetrics { get; } = file.Bind("Performance", "DisableVolumetrics", false, "Disables volumetrics in the game, which significantly improves performance, but removes all fog and may be considered cheating.");

        // Input configuration

        public ConfigEntry<TurnProviderOption> TurnProvider = file.Bind("Input", "TurnProvider", TurnProviderOption.Snap, new ConfigDescription($"Specify which turning provider your player uses, if any.", new AcceptableValueEnum<TurnProviderOption>()));
        public ConfigEntry<float> SmoothTurnSpeedModifier { get; } = file.Bind("Input", "SmoothTurnSpeedModifier", 1f, "A multiplier that is added to the smooth turning speed. Requires turn provider to be set to smooth.");
        public ConfigEntry<float> SnapTurnSize { get; } = file.Bind("Input", "SnapTurnSize", 45f, "The amount of rotation that is applied when performing a snap turn. Requires turn provider to be set to snap.");
        public ConfigEntry<float> SpectateCameraSpeedModifier { get; } = file.Bind("Input", "SpectateCameraSpeedModifier", 2f, "Specifies how fast the camera should pivot around a spectated player.");
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
        public ConfigEntry<float> CustomCameraFOV { get; } = file.Bind("Rendering", "CustomCameraFOV", 90f, "The field of view that the custom camera should have.");
        public ConfigEntry<float> CustomCameraLerpFactor { get; } = file.Bind("Rendering", "CustomCameraLerpFactor", 0.1f, new ConfigDescription("The smoothing factor of the custom camera rotation. Higher values mean more static movement, lower values are more smooth.", new AcceptableValueRange<float>(0.01f, 1f)));
        public ConfigEntry<float> LODBias { get; } = file.Bind("Rendering", "LODBias", 2f, "The LOD bias is a multiplier that dictates when an LOD must reduce their quality. Higher values means that more detailed LODs will persist for longer. Set to -1 to disable updating the LOD bias.");
        public ConfigEntry<bool> DisableLensDistortion { get; } = file.Bind("Rendering", "DisableLensDistortion", false, "Disables the warping effects that you experience when you are under water, use the TZP-inhalant and more.");

        // Tips configuration

        public ConfigEntry<bool> FirstTimeTipSeen { get; } = file.Bind("Tips", "FirstTimeTipSeen", false, "Whether or not the user has seen the first time playing tip");
        public ConfigEntry<bool> ShovelTipSeen { get; } = file.Bind("Tips", "ShovelTipSeen", false, "Whether or not the user has seen the shovel tip");
        public ConfigEntry<bool> SprayPaintTipSeen { get; } = file.Bind("Tips", "SprayPaintTipSeen", false, "Whether or not the user has seen the spray paint tip");

        // Internal configuration
        public ConfigEntry<string> LastInternalControllerProfile { get; } = file.Bind("Internal", "LastInternalControllerProfile", "", "FOR INTERNAL USE ONLY, DO NOT EDIT");

        public enum TurnProviderOption
        {
            Snap,
            Smooth,
            Disabled
        }
    }

    internal class AcceptableValueEnum<T> : AcceptableValueBase where T : notnull, Enum
    {
        private readonly string[] names;

        public AcceptableValueEnum() : base(typeof(T))
        {
            names = Enum.GetNames(typeof(T));
        }

        public override object Clamp(object value) => value;
        public override bool IsValid(object value) => true;
        public override string ToDescriptionString() => $"# Acceptable values: {string.Join(", ", names)}";

    }
}
