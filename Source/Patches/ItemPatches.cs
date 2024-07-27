using HarmonyLib;
using LCVR.Items;
using LCVR.Player;
using UnityEngine;

namespace LCVR.Patches;

[LCVRPatch(LCVRPatchTarget.Universal)]
[HarmonyPatch]
internal static class UniversalItemPatches
{
    /// <summary>
    /// Prevents the built in LateUpdate if a VR item disables it
    /// </summary>
    [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.LateUpdate))]
    [HarmonyPrefix]
    private static bool LateUpdatePrefix(GrabbableObject __instance)
    {
        if (VRItem<GrabbableObject>.itemCache.TryGetValue(__instance, out var item))
            return !item.CancelGameUpdate;

        return true;
    }

    /// <summary>
    /// Updates radar position of the item if the original LateUpdate function got blocked
    /// </summary>
    [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.LateUpdate))]
    [HarmonyPostfix]
    private static void LateUpdatePostfix(GrabbableObject __instance, bool __runOriginal)
    {
        if (!__runOriginal && __instance.radarIcon != null)
            __instance.radarIcon.position = __instance.transform.position;
    }

    [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.GetItemFloorPosition))]
    [HarmonyPrefix]
    private static void GetItemFloorPositionFromHand(ref Vector3 startPosition)
    {
        if (startPosition == Vector3.zero)
            startPosition = VRSession.Instance.LocalPlayer.RightHandVRTarget.position;
    }
}
