using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine.Rendering.HighDefinition;

namespace LCVR.Patches;

[HarmonyPatch]
[LCVRPatch]
internal static class IngamePlayerSettingsPatches
{
    /// <summary>
    /// Prevent the game from interacting with the pre-init scene since we completely replace it
    /// </summary>
    [HarmonyPatch(typeof(IngamePlayerSettings), nameof(IngamePlayerSettings.waitToLoadSettings), MethodType.Enumerator)]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> IgnorePreInitScript(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchForward(false, new CodeMatch(OpCodes.Stloc_3))
            .Advance(-1)
            .SetOpcodeAndAdvance(OpCodes.Nop)
            .RemoveInstructions(15)
            .InstructionEnumeration();
    }

    /// <summary>
    /// Disable motion blur
    /// </summary>
    [HarmonyPatch(typeof(IngamePlayerSettings), nameof(IngamePlayerSettings.SetMotionBlur))]
    [HarmonyPostfix]
    private static void DisableMotionBlur(IngamePlayerSettings __instance)
    {
        if (__instance.universalVolume.sharedProfile.TryGet<MotionBlur>(out var motionBlur))
            motionBlur.active = false;
    }
}