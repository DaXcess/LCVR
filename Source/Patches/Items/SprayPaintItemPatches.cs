using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using LCVR.Player;
using UnityEngine;
using static HarmonyLib.AccessTools;

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

    /// <summary>
    /// Makes the weed killer bottle spray raycast from the bottle instead of the gameplay camera
    /// </summary>
    [HarmonyPatch(typeof(SprayPaintItem), nameof(SprayPaintItem.TrySprayingWeedKillerBottle))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> WeedKillerSprayFromHand(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .Advance(1)
            .RemoveInstructions(13)
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Call, PropertyGetter(typeof(VRSession), nameof(VRSession.Instance))),
                new CodeInstruction(OpCodes.Callvirt, PropertyGetter(typeof(VRSession), nameof(VRSession.LocalPlayer))),
                new CodeInstruction(OpCodes.Callvirt,
                    PropertyGetter(typeof(VRPlayer), nameof(VRPlayer.PrimaryController))),
                new CodeInstruction(OpCodes.Callvirt,
                    PropertyGetter(typeof(VRController), nameof(VRController.InteractOrigin))),
                new CodeInstruction(OpCodes.Callvirt, PropertyGetter(typeof(Transform), nameof(Transform.position)))
            )
            .Advance(2)
            .RemoveInstructions(5)
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Call, PropertyGetter(typeof(VRSession), nameof(VRSession.Instance))),
                new CodeInstruction(OpCodes.Callvirt, PropertyGetter(typeof(VRSession), nameof(VRSession.LocalPlayer))),
                new CodeInstruction(OpCodes.Callvirt,
                    PropertyGetter(typeof(VRPlayer), nameof(VRPlayer.PrimaryController))),
                new CodeInstruction(OpCodes.Callvirt,
                    PropertyGetter(typeof(VRController), nameof(VRController.InteractOrigin))),
                new CodeInstruction(OpCodes.Callvirt, PropertyGetter(typeof(Transform), nameof(Transform.forward)))
            )
            .InstructionEnumeration();
    }
}
