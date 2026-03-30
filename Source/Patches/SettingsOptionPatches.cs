using HarmonyLib;
using TMPro;

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

    /// <summary>
    /// Disable the Motion Blur option in the settings menu
    /// </summary>
    [HarmonyPatch(typeof(SettingsOption), nameof(SettingsOption.OnEnable))]
    [HarmonyPostfix]
    private static void DisableMotionBlurOption(SettingsOption __instance)
    {
        if (__instance.optionType != SettingsOptionType.MotionBlur)
            return;

        var dropdown = __instance.GetComponent<TMP_Dropdown>();
        dropdown.interactable = false;
        dropdown.enabled = false;
        dropdown.captionText.text = "Absolutely not";
    }
}
