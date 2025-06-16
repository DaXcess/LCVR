using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using LCVR.Managers;
using LCVR.Player;
using UnityEngine;
using static HarmonyLib.AccessTools;

namespace LCVR.Patches.Items;

[LCVRPatch]
[HarmonyPatch]
internal static class SprayPaintItemPatches
{
    /// <summary>
    /// Offset to make the spray paint come out of the tip of the can instead of from the hand
    /// </summary>
    private static readonly Vector3 SprayOffset = new(-0.1025f, 0.225f, 0.16f);

    /// <summary>
    /// Offset to make the weed killer spray come out of the tip of the bottle instead of from the hand
    /// </summary>
    private static readonly Vector3 WeedOffset = new(-0.04f, 0.16f, 0.3f);
    
    /// <summary>
    /// Makes the spray paint item spray from your hand instead of your head
    /// </summary>
    [HarmonyPatch(typeof(SprayPaintItem), nameof(SprayPaintItem.TrySpraying))]
    [HarmonyPrefix]
    private static bool SprayPaintFromHand(SprayPaintItem __instance, ref bool __result)
    {
        var rayOrigin = VRSession.Instance.LocalPlayer.PrimaryController.InteractOrigin;
        var position = rayOrigin.TransformPoint(SprayOffset);

        if (__instance.AddSprayPaintLocal(position, rayOrigin.forward))
        {
            __instance.SprayPaintServerRpc(position, rayOrigin.forward);
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
                new CodeInstruction(OpCodes.Call, ((Func<Vector3>)GetSprayPosition).Method)
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

        static Vector3 GetSprayPosition()
        {
            return VRSession.Instance.LocalPlayer.PrimaryController.InteractOrigin.TransformPoint(WeedOffset);
        }
    }
}
