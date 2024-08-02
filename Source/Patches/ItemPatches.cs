using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using LCVR.Items;
using LCVR.Player;
using UnityEngine;
using static HarmonyLib.AccessTools;

namespace LCVR.Patches;

[LCVRPatch]
[HarmonyPatch]
internal static class ItemPatches
{
    /// <summary>
    /// When dropping items, drop them from the real life hand position instead of the (constrained) in-game hand position
    /// (Only when dropping from a larger distance)
    /// </summary>
    [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.GetItemFloorPosition))]
    [HarmonyPrefix]
    private static void GetItemFloorPositionFromHand(ref Vector3 startPosition)
    {
        var handPos = VRSession.Instance.LocalPlayer.RightHandVRTarget.position;
        var playerPos = VRSession.Instance.LocalPlayer.transform.position;
        
        var handXZ = new Vector3(handPos.x, 0, handPos.z);
        var playerXZ = new Vector3(playerPos.x, 0, playerPos.z);
        
        if (startPosition == Vector3.zero && Vector3.Distance(handXZ, playerXZ) > 0.6f)
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

    /// <summary>
    /// Prevent the spray paint item from calling "DiscardItem" too early
    /// </summary>
    [HarmonyPatch(typeof(SprayPaintItem), nameof(SprayPaintItem.DiscardItem))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> SprayPaintMoveDiscard(IEnumerable<CodeInstruction> instructions)
    {
        return instructions.Skip(2).AddItem(new CodeInstruction(OpCodes.Ldarg_0)).AddItem(
            new CodeInstruction(OpCodes.Callvirt,
                Method(typeof(GrabbableObject), nameof(GrabbableObject.DiscardItem))));
    }

    /// <summary>
    /// Correct the "equippedUsableItemQE" field when the walkie talkie is pocketed
    /// </summary>
    [HarmonyPatch(typeof(WalkieTalkie), nameof(WalkieTalkie.PocketItem))]
    [HarmonyPostfix]
    private static void OnPocketWalkie(WalkieTalkie __instance)
    {
        if (__instance.playerHeldBy != null)
            __instance.playerHeldBy.equippedUsableItemQE = false;
    }
}
