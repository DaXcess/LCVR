using HarmonyLib;
using LCVR.Player;

namespace LCVR.Patches.Items;

[LCVRPatch]
[HarmonyPatch]
internal static class SprayPaintItemPatches
{
    /// <summary>
    /// Makes the spray paint item spray from your hand instead of your head
    /// </summary>
    [HarmonyPatch(typeof(SprayPaintItem), nameof(SprayPaintItem.TrySpraying))]
    [HarmonyPrefix]
    private static bool SprayPaintFromHand(SprayPaintItem __instance, ref bool __result)
    {
        var rayOrigin = VRSession.Instance.LocalPlayer.PrimaryController.InteractOrigin;

        if (__instance.AddSprayPaintLocal(rayOrigin.transform.position, rayOrigin.transform.forward))
        {
            __instance.SprayPaintServerRpc(rayOrigin.transform.position, rayOrigin.transform.forward);
            __result = true;
            return false;
        }

        __result = false;

        return false;
    }
}
