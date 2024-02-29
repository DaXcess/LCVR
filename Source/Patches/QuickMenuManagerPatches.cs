using HarmonyLib;
using LCVR.Player;

namespace LCVR.Patches;

[LCVRPatch]
[HarmonyPatch]
internal static class QuickMenuManagerPatches
{
    [HarmonyPatch(typeof(QuickMenuManager), "OpenQuickMenu")]
    [HarmonyPostfix]
    private static void AfterOpenPauseMenu()
    {
        VRSession.Instance.OnPauseMenuOpened();
    }

    [HarmonyPatch(typeof(QuickMenuManager), "CloseQuickMenu")]
    [HarmonyPostfix]
    private static void AfterClosePauseMenu()
    {
        VRSession.Instance.OnPauseMenuClosed();
    }
}
