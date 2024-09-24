using HarmonyLib;
using LCVR.Networking;
using LCVR.Player;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace LCVR.Patches;

[LCVRPatch(LCVRPatchTarget.Universal)]
[HarmonyPatch]
internal static class GameNetworkManagerPatches
{
    /// <summary>
    /// Fix some bogus crashes
    /// </summary>
    [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.StartDisconnect))]
    [HarmonyPrefix]
    private static void OnLeaveGame()
    {
        if (!VRSession.Instance)
            return;

        if (VRSession.InVR)
            Object.DestroyImmediate(VRSession.Instance.LocalPlayer);

        if (!NetworkSystem.Instance)
            return;

        foreach (var player in NetworkSystem.Instance.Players)
            player.GetComponent<RigBuilder>().enabled = false;
    }
}
