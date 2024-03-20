using HarmonyLib;

namespace LCVR.Patches.Items;

[LCVRPatch]
[HarmonyPatch]
internal static class ShovelItemPatches
{
    /// <summary>
    /// Prevent being able to use the trigger to swing the shovel
    /// </summary>
    [HarmonyPatch(typeof(Shovel), nameof(Shovel.ItemActivate))]
    [HarmonyPrefix]
    private static bool ItemActivate()
    {
        return false;
    }
}
