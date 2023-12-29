using HarmonyLib;
using LCVR.Items;

namespace LCVR.Patches
{
    [LCVRPatch(LCVRPatchTarget.Universal)]
    [HarmonyPatch]
    internal static class UniversalItemPatches
    {
        [HarmonyPatch(typeof(GrabbableObject), "LateUpdate")]
        [HarmonyPrefix]
        private static bool LateUpdatePrefix(GrabbableObject __instance)
        {
            if (VRItem<GrabbableObject>.itemCache.TryGetValue(__instance, out var item))
                return !item.CancelGameUpdate;

            return true;
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
