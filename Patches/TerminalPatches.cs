using HarmonyLib;
using LCVR.Player;
using UnityEngine;

namespace LCVR.Patches
{
    [LCVRPatch]
    [HarmonyPatch]
    internal class TerminalPatches
    {
        [HarmonyPatch(typeof(Terminal), "BeginUsingTerminal")]
        [HarmonyPostfix]
        private static void OnEnterTerminal(Terminal __instance)
        {
            // TODO: Remove
            __instance.groupCredits = 2500;
            __instance.SyncGroupCreditsServerRpc(2500, __instance.numberOfItemsInDropship);

            var player = Object.FindObjectOfType<VRPlayer>();

            player.OnEnterTerminal();
        }

        [HarmonyPatch(typeof(Terminal), "QuitTerminal")]
        [HarmonyPostfix]
        private static void OnExitTerminal()
        {
            var player = Object.FindObjectOfType<VRPlayer>();

            player.OnExitTerminal();
        }
    }
}
