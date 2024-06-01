using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using LCVR.Items;
using UnityEngine;
using static HarmonyLib.AccessTools;

namespace LCVR.Patches.Items;

[LCVRPatch]
[HarmonyPatch]
internal static class KnifeItemPatches
{
    [HarmonyPatch(typeof(KnifeItem), nameof(KnifeItem.HitKnife))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> HitKnifeVRPatch(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchForward(false,
            [
                new CodeMatch(OpCodes.Call,
                    Method(typeof(UnityEngine.Physics), nameof(UnityEngine.Physics.SphereCastAll),
                    [
                        typeof(Vector3), typeof(float), typeof(Vector3), typeof(float), typeof(int),
                        typeof(QueryTriggerInteraction)
                    ]))
            ])
            .Advance(-23)
            .RemoveInstructions(24)
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, Method(typeof(VRKnife), nameof(VRKnife.GetKnifeHits)))
            )
            .InstructionEnumeration();
    }
}
