using HarmonyLib;

namespace LCVR.Patches.Items;

[LCVRPatch]
[HarmonyPatch]
internal static class ShovelItemPatches
{
    /// <summary>
    /// Prevent being able to use the trigger to swing the shovel
    ///
    /// TODO: Replace with this special code that temporarily disables arm tracking
    /// </summary>
    [HarmonyPatch(typeof(Shovel), nameof(Shovel.ItemActivate))]
    [HarmonyPrefix]
    private static bool ItemActivate()
    {
        return true;
    }
}
