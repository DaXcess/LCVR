using HarmonyLib;
using LCVR.Input;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine.InputSystem;
using static HarmonyLib.AccessTools;

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


            int startIndex = codes.FindIndex(x => x.opcode == OpCodes.Brtrue_S);
            int endIndex = codes.FindIndex(x => x.operand == (object)Method(typeof(InputActionRebindingExtensions), nameof(InputActionRebindingExtensions.LoadBindingOverridesFromJson)));

            for (var i = startIndex; i <= endIndex; i++)
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

            int index = codes.FindLastIndex(x => x.opcode == OpCodes.Ldstr);

            codes[index].operand = Properties.Resources.lc_inputs;

            return codes.AsEnumerable();
        }
    }
}
