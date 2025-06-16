using HarmonyLib;
using LCVR.UI;
using UnityEngine;

namespace LCVR.Patches.Items;

[LCVRPatch]
[HarmonyPatch]
internal static class ShotgunItemPatches
{
    /// <summary>
    /// Makes the shotgun shoot from your hand instead of your head
    /// </summary>
    [HarmonyPatch(typeof(ShotgunItem), nameof(ShotgunItem.ShootGunAndSync))]
    [HarmonyPrefix]
    private static bool OnShootGun(ShotgunItem __instance, bool heldByPlayer)
    {
        if (!heldByPlayer)
            return true;

        var position = __instance.shotgunRayPoint.TransformPoint(0, -0.0807f, 3.0816f);
        var forward = __instance.shotgunRayPoint.forward;
        
        __instance.ShootGun(position, forward);
        __instance.localClientSendingShootGunRPC = true;
        __instance.ShootGunServerRpc(position, forward);

        return false;
    }

    /// <summary>
    /// Display a popup informing the player whether safety is on or not
    /// </summary>
    [HarmonyPatch(typeof(ShotgunItem), nameof(ShotgunItem.SetSafetyControlTip))]
    [HarmonyPostfix]
    private static void DisplaySafetyPatch(ShotgunItem __instance)
    {
        if (!__instance.IsOwner)
            return;

        PopupText.Create(__instance.transform, Vector3.up * 0.25f, $"SAFETY: {(__instance.safetyOn ? "ON" : "OFF")}",
            1.5f);
    }
}