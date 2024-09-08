using HarmonyLib;
using LCVR.Networking;
using LCVR.Player;
using UnityEngine;

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
    /// Handle setting custom VR item offsets, while still allowing non-VR offsets to function normally
    /// </summary>
    [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.LateUpdate))]
    [HarmonyPrefix]
    private static bool ItemUpdateOffset(GrabbableObject __instance)
    {
        return HandleUpdateItemOffset(__instance);
    }
    
    /// <summary>
    /// Cave creature is different so need a separate patch
    /// </summary>
    [HarmonyPatch(typeof(CaveDwellerPhysicsProp), nameof(CaveDwellerPhysicsProp.LateUpdate))]
    [HarmonyPrefix]
    private static bool ItemUpdateOffset(CaveDwellerPhysicsProp __instance)
    {
        return !__instance.caveDwellerScript.inSpecialAnimation || HandleUpdateItemOffset(__instance);
    }

    private static bool HandleUpdateItemOffset(GrabbableObject item)
    {
        var isLocalPlayer = item.playerHeldBy == StartOfRound.Instance.localPlayerController;
        
        // Don't set custom offset if local player is not in VR
        if (isLocalPlayer && !VRSession.InVR)
            return true;

        // If the item isn't held, we don't care
        if (item.playerHeldBy == null || (item.playerHeldBy == StartOfRound.Instance.localPlayerController &&
                                          item.playerHeldBy.currentlyHeldObjectServer != item))
            return true;

        // Don't set custom offset if remote player is not in VR
        if (!isLocalPlayer && !NetworkSystem.Instance.IsInVR((ushort)item.playerHeldBy.playerClientId))
            return true;

        // Prevent shovels from updating item offset as we're using our own implementation
        if (item.GetType() == typeof(Shovel))
            return false;
        
        // Don't set custom offset if item does not have a custom offset
        if (!Player.Items.itemOffsets.TryGetValue(item.itemProperties.itemName, out var offset))
            return true;

        var (positionOffset, rotationOffset) = offset;

        if (item.parentObject == null)
            return false;

        var tf = item.transform;

        tf.rotation = item.parentObject.rotation;
        tf.Rotate(rotationOffset);
        tf.position = item.parentObject.position + item.parentObject.rotation * positionOffset;

        return false;
    }

    /// <summary>
    /// Make sure to set the radar icon position if needed
    /// </summary>
    [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.LateUpdate))]
    [HarmonyPostfix]
    private static void PostfixSetRadarIcon(GrabbableObject __instance)
    {
        if (__instance.radarIcon != null)
            __instance.radarIcon.position = __instance.transform.position;
    }
}
