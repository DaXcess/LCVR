using HarmonyLib;
using LCVR.Input;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace LCVR.Patches
{
    [LCVRPatch]
    [HarmonyPatch]
    public class InputPatches
    {
        [HarmonyPatch(typeof(IngamePlayerSettings), nameof(IngamePlayerSettings.LoadSettingsFromPrefs))]
        [HarmonyPostfix]
        private static void OnLoadSettings(IngamePlayerSettings __instance)
        {
            Actions.ReloadInputBindings();
        }
    }

    [LCVRPatch]
    [HarmonyPatch(typeof(IngamePlayerSettings), "DiscardChangedSettings")]
    internal static class InputPatches_DiscardChangedSettings
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            for (var i = 15; i <= 27; i++)
            {
                codes[i].opcode = OpCodes.Nop;
                codes[i].operand = null;
            }

            return codes.AsEnumerable();
        }
    }

    [LCVRPatch]
    [HarmonyPatch(typeof(PlayerActions))]
    [HarmonyPatch(MethodType.Constructor)]
    internal static class PlayerActionsPatches
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            codes[6].operand = Properties.Resources.lc_inputs;

            return codes.AsEnumerable();
        }
    }
}
