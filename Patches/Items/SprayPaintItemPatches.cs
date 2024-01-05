using HarmonyLib;
using LCVR.Player;
using System.Reflection;
using UnityEngine;

namespace LCVR.Patches.Items
{
    [LCVRPatch]
    [HarmonyPatch]
    internal static class SprayPaintItemPatches
    {
        /// <summary>
        /// Makes the spray paint item spray from your hand instead of your head
        /// </summary>
        [HarmonyPatch(typeof(SprayPaintItem), "TrySpraying")]
        [HarmonyPrefix]
        private static bool SprayPaintFromHand(SprayPaintItem __instance, ref bool __result)
        {
            var rayOrigin = Object.FindObjectOfType<VRController>().interactOrigin;

            if ((bool)typeof(SprayPaintItem).GetMethod("AddSprayPaintLocal", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, [rayOrigin.transform.position, rayOrigin.transform.forward]))
            {
                __instance.SprayPaintServerRpc(rayOrigin.transform.position, rayOrigin.transform.forward);
                __result = true;
                return false;
            }

            __result = false;

            return false;
        }
    }
}
