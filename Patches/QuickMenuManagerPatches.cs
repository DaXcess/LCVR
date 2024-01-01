using HarmonyLib;
using LCVR.Player;
using UnityEngine;

namespace LCVR.Patches
{
    [LCVRPatch]
    [HarmonyPatch]
    internal static class QuickMenuManagerPatches
    {
        [HarmonyPatch(typeof(QuickMenuManager), "OpenQuickMenu")]
        [HarmonyPostfix]
        private static void AfterOpenPauseMenu()
        {
            var player = Object.FindObjectOfType<VRPlayer>();

            player.OnPauseMenuOpened();
        }

        [HarmonyPatch(typeof(QuickMenuManager), "CloseQuickMenu")]
        [HarmonyPostfix]
        private static void AfterClosePauseMenu()
        {
            var player = Object.FindObjectOfType<VRPlayer>();

            player.OnPauseMenuClosed();
        }
    }
}
