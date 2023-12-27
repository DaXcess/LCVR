using BepInEx.Configuration;
using System;

namespace LCVR
{
    public class Config(ConfigFile file)
    {
        // General configuration

        public ConfigEntry<bool> DisableVR { get; } = file.Bind("General", "DisableVR", false, "Disables the main functionality of this mod, can be used if you want to play without VR while keeping the mod installed.");
        public ConfigEntry<bool> IntroScreenSeen { get; } = file.Bind("General", "IntroScreenSeen", false, "Whether the VR intro screen has been displayed before. This configuration option should be set automatically.");

        // Performance configuration

        public ConfigEntry<bool> EnableUpscaling { get; } = file.Bind("Performance", "EnableUpscaling", false, "This setting enables 'Dynamic Resolution' in the HDRP pipeline, which is required for upscaling.");
        public ConfigEntry<bool> EnableDLLS { get; } = file.Bind("Performance", "EnableDLLS", false, "Enable DLLS support for the game. Requires upscaling to be enabled.");
        public ConfigEntry<int> ResolutionPercentage { get; } = file.Bind("Performance", "ResolutionPercentage", 80, new ConfigDescription("The resolution to render the game on, which will then be upsampled. Requires upscaling to be enabled.", new AcceptableValueRange<int>(0, 100)));
        public ConfigEntry<bool> DisableVolumetrics { get; } = file.Bind("Performance", "DisableVolumetrics", false, "Disables volumetrics in the game, which significantly improves performance, but removes all fog and may be considered cheating.");

        // Input configuration

        private ConfigEntry<string> _turnProvider = file.Bind("Input", "TurnProvider", "Snap", new ConfigDescription("Specify which turning provider your player uses, if any.", new AcceptableValueList<string>("Snap", "Smooth", "Disabled")));
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
        public ConfigEntry<bool> EnableInteractRay { get; } = file.Bind("UI", "EnableInteractRay", false, "Enable a visible ray coming out of the hand that tells you where you are currently aiming at. More or less for debug purposes, but can be used to get used to the rotations and such.");

        public enum TurnProviderOption
        {
            Snap,
            Smooth,
            Disabled
        }
    }
}
