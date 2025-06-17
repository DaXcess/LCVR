using HarmonyLib;
using LCVR.Patches;
using MoreCompany.Cosmetics;

namespace LCVR.Compatibility.MoreCompany;

[LCVRPatch(dependency: Compat.MoreCompany)]
[HarmonyPatch]
internal static class MoreCompanyUIPatches
{
    /// <summary>
    /// Yeah... I'm completely lost on this one
    /// </summary>
    [HarmonyPatch(typeof(CosmeticRegistry), nameof(CosmeticRegistry.UpdateCosmeticsOnDisplayGuy))]
    [HarmonyPostfix]
    private static void AfterUpdateCosmetics()
    {
        CosmeticRegistry.displayGuyCosmeticApplication.spawnedCosmetics.Do(cosmetic =>
            cosmetic.transform.localScale *= CosmeticRegistry.menuIsInGame ? 1.75f : 0.45f);
    }
}