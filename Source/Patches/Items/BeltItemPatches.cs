using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using GameNetcodeStuff;
using HarmonyLib;
using LCVR.Managers;
using UnityEngine;

using static HarmonyLib.AccessTools;

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

    /// <summary>
    /// Make the belt check for items from your hand instead of your head
    /// </summary>
    [HarmonyPatch(typeof(BeltBagItem), nameof(BeltBagItem.ItemInteractLeftRight))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> RayFromHandPatch(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchForward(false,
                new CodeMatch(OpCodes.Ldfld,
                    Field(typeof(PlayerControllerB), nameof(PlayerControllerB.gameplayCamera))))
            .Repeat(matcher =>
                matcher.Set(OpCodes.Call, ((Func<PlayerControllerB, Transform>)GetPlayerInteractTransform).Method))
            .InstructionEnumeration();

        static Transform GetPlayerInteractTransform(PlayerControllerB player) => player.IsLocalPlayer()
            ? VRSession.Instance.LocalPlayer.PrimaryController.InteractOrigin
            : player.gameplayCamera.transform;
    }
}
