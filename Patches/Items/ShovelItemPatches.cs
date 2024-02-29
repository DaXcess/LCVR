using HarmonyLib;

namespace LCVR.Patches.Items;

[LCVRPatch]
[HarmonyPatch]
internal static class ShovelItemPatches
{
    [HarmonyPatch(typeof(Shovel), "ItemActivate")]
    [HarmonyPrefix]
    private static bool ItemActivate()
    {
        return false;
    }
}
