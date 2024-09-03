using HarmonyLib;

namespace LCVR.Patches.Items;

[LCVRPatch]
[HarmonyPatch]
internal static class BeltItemPatches
{
    /// <summary>
    /// Prevent the "Open Bag" interaction trigger from colliding with the interact laser
    /// </summary>
    [HarmonyPatch(typeof(BeltBagItem), nameof(BeltBagItem.EquipItem))]
    [HarmonyPostfix]
    private static void OnEquipBag(BeltBagItem __instance)
    {
        __instance.GetComponentInChildren<InteractTrigger>().enabled = false;
    }
    
    /// <summary>
    /// Re-enable the "Open Bag" trigger when dropping the item
    /// </summary>
    [HarmonyPatch(typeof(BeltBagItem), nameof(BeltBagItem.DiscardItem))]
    [HarmonyPostfix]
    private static void OnDiscardBag(BeltBagItem __instance)
    {
        __instance.GetComponentInChildren<InteractTrigger>().enabled = true;
    }

    /// <summary>
    /// Allow the trigger button to also close the bag
    /// </summary>
    [HarmonyPatch(typeof(BeltBagItem), nameof(BeltBagItem.ItemActivate))]
    [HarmonyPrefix]
    private static bool CloseBagPatch(BeltBagItem __instance, bool buttonDown)
    {
        if (!buttonDown)
            return false;
        
        if (__instance.currentPlayerChecking != StartOfRound.Instance.localPlayerController)
            return true;

        StartOfRound.Instance.localPlayerController.SetInSpecialMenu(false);
        return false;
    }
}
