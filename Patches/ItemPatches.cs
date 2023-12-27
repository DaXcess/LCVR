using HarmonyLib;

namespace LCVR.Patches
{
    [LCVRPatch]
    [HarmonyPatch]
    internal static class ItemPatches
    {
        [HarmonyPatch(typeof(GrabbableObject), "LateUpdate")]
        [HarmonyPrefix]
        private static bool LateUpdatePrefix(GrabbableObject __instance)
        {
            var cancel = __instance.itemProperties.itemName switch
            {
                "Shovel" or "Stop sign" or "Yield sign" => true,
                _ => false
            };

            return !cancel;
        }

        [HarmonyPatch(typeof(GrabbableObject), "LateUpdate")]
        [HarmonyPostfix]
        private static void LateUpdatePostfix(GrabbableObject __instance, bool __runOriginal)
        {
            if (!__runOriginal && __instance.radarIcon != null)
                __instance.radarIcon.position = __instance.transform.position;
        }
    }
}
