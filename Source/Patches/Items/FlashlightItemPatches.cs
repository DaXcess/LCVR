using HarmonyLib;
using LCVR.Networking;

namespace LCVR.Patches.Items;

[LCVRPatch(LCVRPatchTarget.Universal)]
[HarmonyPatch]
public static class FlashlightItemPatches
{
    /// <summary>
    /// If the player is a VR player, use the light from the flashlight item instead of the helmet light to allow
    /// for the light to be moved along with the flashlight object
    /// </summary>
    [HarmonyPatch(typeof(FlashlightItem), nameof(FlashlightItem.SwitchFlashlight))]
    [HarmonyPostfix]
    private static void SwitchFlashlightPatch(FlashlightItem __instance, bool on)
    {
        // If it's our flashlight, ignore
        if (__instance.IsOwner || !__instance.playerHeldBy)
            return;

        if (!NetworkSystem.Instance.IsInVR((ushort)__instance.playerHeldBy.playerClientId))
            return;

        __instance.flashlightBulb.enabled = on;
        __instance.flashlightBulbGlow.enabled = on;
        __instance.playerHeldBy.ChangeHelmetLight(__instance.flashlightTypeID, false);
    }
    
    /// <summary>
    /// Make sure to enable the "helmet" flashlight beam if a VR player pockets their flashlight while it's still active
    /// </summary>
    [HarmonyPatch(typeof(FlashlightItem), nameof(FlashlightItem.PocketFlashlightClientRpc))]
    [HarmonyPostfix]
    private static void PocketFlashlightPatch(FlashlightItem __instance, bool stillUsingFlashlight)
    {
        if (!NetworkSystem.Instance.IsInVR((ushort)__instance.previousPlayerHeldBy.playerClientId))
            return;

        __instance.previousPlayerHeldBy.ChangeHelmetLight(__instance.flashlightTypeID, stillUsingFlashlight);
    }
}
