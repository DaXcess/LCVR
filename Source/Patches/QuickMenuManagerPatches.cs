using HarmonyLib;
using LCVR.Player;

namespace LCVR.Patches;

[LCVRPatch]
[HarmonyPatch]
internal static class QuickMenuManagerPatches
{
    /// <summary>
    /// Detech when the pause menu opens
    /// </summary>
    [HarmonyPatch(typeof(QuickMenuManager), nameof(QuickMenuManager.OpenQuickMenu))]
    [HarmonyPostfix]
    private static void AfterOpenPauseMenu()
    {
        VRSession.Instance.OnPauseMenuOpened();
    }

    /// <summary>
    /// Detech when the pause menu closes
    /// </summary>
    [HarmonyPatch(typeof(QuickMenuManager), nameof(QuickMenuManager.CloseQuickMenu))]
    [HarmonyPostfix]
    private static void AfterClosePauseMenu()
    {
        VRSession.Instance.OnPauseMenuClosed();
    }
}
