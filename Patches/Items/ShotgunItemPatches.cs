using HarmonyLib;
using LCVR.Player;
using System.Reflection;
using UnityEngine;

namespace LCVR.Patches.Items
{
    [LCVRPatch]
    [HarmonyPatch]
    internal static class ShotgunItemPatches
    {
        [HarmonyPatch(typeof(ShotgunItem), "ShootGunAndSync")]
        [HarmonyPrefix]
        private static bool OnShootGun(ShotgunItem __instance, bool heldByPlayer)
        {
            if (!heldByPlayer)
                return true;

            var rayOrigin = Object.FindObjectOfType<VRController>().interactOrigin;
            __instance.ShootGun(rayOrigin.position, rayOrigin.forward);
            typeof(ShotgunItem).GetField("localClientSendingShootGunRPC", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(__instance, true);
            __instance.ShootGunServerRpc(rayOrigin.position, rayOrigin.forward);

            return false;
        }
    }
}
