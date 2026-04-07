#if DEBUG
using HarmonyLib;
using LCVR.Managers;
using UnityEngine;

namespace LCVR.Patches;

[LCVRPatch(LCVRPatchTarget.Universal)]
[HarmonyPatch]
internal static class MenuManagerPatches
{
    [HarmonyPatch(typeof(MenuManager), nameof(MenuManager.Start))]
    [HarmonyPostfix]
    private static void OnMenuStart()
    {
        if (GameNetworkManager.Instance.disableSteam)
            return;

        new GameObject("LCVR Log Sharing Manager").AddComponent<LogSharingManager>();
    }
}
#endif