using HarmonyLib;
using LCVR.Items;
using LCVR.Player;
using UnityEngine;

namespace LCVR.Patches;

[LCVRPatch]
[HarmonyPatch]
internal static class ItemPatches
{
    /// <summary>
    /// Make the items drop at the real life hand position instead of the item's current position to make dropping easier
    /// </summary>
    [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.GetItemFloorPosition))]
    [HarmonyPrefix]
    private static void GetItemFloorPositionFromHand(ref Vector3 startPosition)
    {
        if (startPosition != Vector3.zero)
            return;

        var player = VRSession.Instance.LocalPlayer;
        var localPosition = player.transform.InverseTransformPoint(player.RightHandVRTarget.position);
            
        // Only apply the logic if the hand is far away enough, otherwise it looks weird on close range
        // TODO: Find a good minimum distance

        var magnitude = localPosition.magnitude;
        Logger.LogDebug($"Drop item magnitude: {magnitude}");
        
        if (magnitude > 0.5)
            startPosition = VRSession.Instance.LocalPlayer.RightHandVRTarget.position;
    }
}

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
}
