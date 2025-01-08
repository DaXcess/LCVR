using HarmonyLib;

namespace LCVR.Patches;

[LCVRPatch]
[HarmonyPatch]
internal static class SettingsOptionPatches
{
    /// <summary>
    /// When you close the rebinding menu, this function makes sure the binding text is reset to normal for each binding.
    /// In VR, we use icons, so this only causes issues for us.
    /// </summary>
    [HarmonyPatch(typeof(SettingsOption), nameof(SettingsOption.SetBindingToCurrentSetting))]
    [HarmonyPrefix]
    private static bool IgnoreBindingTextReload()
    {
        return false;
    }
}
