using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

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
}