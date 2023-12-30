using BepInEx.Configuration;
using System;

namespace LCVR
{
    public class Config(ConfigFile file)
    {
        // General configuration

        public ConfigEntry<bool> DisableVR { get; } = file.Bind("General", "DisableVR", false, "Disables the main functionality of this mod, can be used if you want to play without VR while keeping the mod installed.");
        public ConfigEntry<bool> IntroScreenSeen { get; } = file.Bind("General", "IntroScreenSeen", false, "Whether the VR intro screen has been displayed before. This configuration option should be set automatically.");
        public ConfigEntry<bool> OverrideCompatibilityVersionCheck { get; } = file.Bind("General", "OverrideCompatibilityVersionCheck", false, "If enabled, ignores the version check when detecting compatible mods. Warning: Enabling this may cause problems if non-standard compatible mod versions are used.");

        // Performance configuration

        public ConfigEntry<bool> EnableUpscaling { get; } = file.Bind("Performance", "EnableUpscaling", false, "This setting enables 'Dynamic Resolution' in the HDRP pipeline, which is required for upscaling.");
        public ConfigEntry<bool> EnableDLSS { get; } = file.Bind("Performance", "EnableDLSS", false, "Enable DLSS support for the game. Requires upscaling to be enabled.");
        public ConfigEntry<int> ResolutionPercentage { get; } = file.Bind("Performance", "ResolutionPercentage", 80, new ConfigDescription("The resolution to render the game on, which will then be upsampled. Requires upscaling to be enabled.", new AcceptableValueRange<int>(0, 100)));
        public ConfigEntry<bool> DisableVolumetrics { get; } = file.Bind("Performance", "DisableVolumetrics", false, "Disables volumetrics in the game, which significantly improves performance, but removes all fog and may be considered cheating.");

        // Input configuration

        private ConfigEntry<string> _turnProvider = file.Bind("Input", "TurnProvider", "Snap", new ConfigDescription("Specify which turning provider your player uses, if any.", new AcceptableValueList<string>("Snap", "Smooth", "Disabled")));
        public ConfigEntry<float> SmoothTurnSpeedModifier { get; } = file.Bind("Input", "SmoothTurnSpeedModifier", 1f, "A multiplier that is added to the smooth turning speed. Requires turn provider to be set to smooth");
        public ConfigEntry<float> SpectateCameraSpeedModifier { get; } = file.Bind("Input", "SpectateCameraSpeedModifier", 2f, "Specifies how fast the camera should pivot around a spectated player.");

        public TurnProviderOption TurnProvider
        {
            get
            {
                if (Enum.TryParse<TurnProviderOption>(_turnProvider.Value, out var option))
                    return option;

                return TurnProviderOption.Snap;
            }
        }

        // UI configuration

        public ConfigEntry<bool> DisableArmHUD { get; } = file.Bind("UI", "DisableArmHUD", false, "Removes the HUD from the arms and displays them in front of the camera.");
        public ConfigEntry<float> HUDOffsetX { get; } = file.Bind("UI", "OffsetX", 0f, "The x offset of the HUD that was placed on the camera instead of the arms. Requires the arm HUD to be disabled.");
        public ConfigEntry<float> HUDOffsetY { get; } = file.Bind("UI", "OffsetY", 0f, "The y offset of the HUD that was placed on the camera instead of the arms. Requires the arm HUD to be disabled.");
        public ConfigEntry<bool> EnableInteractRay { get; } = file.Bind("UI", "EnableInteractRay", false, "Enable a visible ray coming out of the hand that tells you where you are currently aiming at. More or less for debug purposes, but can be used to get a feel for the rotations and such.");

        // Rendering configuration

        public ConfigEntry<bool> EnableCustomCamera { get; } = file.Bind("Rendering", "EnableCustomCamera", false, "Adds a second camera mounted on top of the VR camera that will render seperately from the VR camera to the monitor. This requires quite a bit of extra GPU power!");
        public ConfigEntry<float> CustomCameraFOV { get; } = file.Bind("Rendering", "CustomCameraFOV", 90f, "The field of view that the custom camera should have.");
        public ConfigEntry<float> LODBias { get; } = file.Bind("Rendering", "LODBias", 2f, "The LOD bias is a multiplier that dictates when an LOD must reduce their quality. Higher values means that more detailed LODs will persist for longer. Set to -1 to disable updating the LOD bias.");

        public enum TurnProviderOption
        {
            Snap,
            Smooth,
            Disabled
        }
    }
}
