using HarmonyLib;

namespace LethalCompanyVR
{
    [HarmonyPatch]
    public class InputPatches
    {
        [HarmonyPatch(typeof(IngamePlayerSettings), nameof(IngamePlayerSettings.LoadSettingsFromPrefs))]
        [HarmonyPostfix]
        private static void OnLoadSettings(IngamePlayerSettings __instance)
        {
            __instance.playerInput.actions.LoadFromJson(Properties.Resources.inputs);
            __instance.playerInput.actions.Enable();

            // Oh my fucking god I love having to spend an entire night only to find out **THIS** is what I needed to do
            __instance.playerInput.enabled = false;
            __instance.playerInput.enabled = true;

            Logger.LogDebug("Loaded XR input binding overrides");
        }
    }
}
