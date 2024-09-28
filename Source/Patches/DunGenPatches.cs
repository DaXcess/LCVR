using System.Collections.Generic;
using System.Reflection.Emit;
using DunGen;
using HarmonyLib;
using UnityEngine;
using static HarmonyLib.AccessTools;

namespace LCVR.Patches;

/// <summary>
/// This is a patch that fixes a problem in DunGen that should actually be done in a separate mod.
///
/// But it is affected debug builds in the VR mod and I am too lazy to make another separate mod to fix it.
/// </summary>
[LCVRPatch(LCVRPatchTarget.Universal)]
[HarmonyPatch]
internal static class DunGenPatches
{
    [HarmonyPatch(typeof(DungeonGenerator), nameof(DungeonGenerator.InnerGenerate), MethodType.Enumerator)]
    [HarmonyTranspiler]
    [HarmonyDebug]
    private static IEnumerable<CodeInstruction> DunGenAllowInfiniteRetries(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchForward(false,
                new CodeMatch(OpCodes.Call, PropertyGetter(typeof(Application), nameof(Application.isEditor))))
            .SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_0))
            .InstructionEnumeration();
    }
}
