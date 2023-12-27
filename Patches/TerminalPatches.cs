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
        private static void OnEnterTerminal()
        {
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

    [LCVRPatch(LCVRPatchTarget.Universal)]
    [HarmonyPatch]
    internal static class UniversalTerminalPatches
    {
        [HarmonyPatch(typeof(Terminal), "BeginUsingTerminal")]
        [HarmonyPostfix]
        private static void OnEnterTerminal()
        {

        }
    }
}
