using HarmonyLib;

namespace LCVR.Patches;

[LCVRPatch]
[HarmonyPatch]
internal static class RoundManagerPatches
{
    /// <summary>
    /// Easter egg
    /// </summary>
    [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.GenerateNewLevelClientRpc))]
    [HarmonyPostfix]
    private static void OnGenerateNewLevelClientRpc()
    {
        var today = System.DateTime.Today;
        if (today.Day == 1 && today.Month == 4)
            HUDManager.Instance.loadingText.text = "Random seed: getfixedboi";
    }
}
