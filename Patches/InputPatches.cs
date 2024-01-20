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
        private static void OnLoadSettings()
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

            int startIndex = codes.FindIndex(x => x.opcode == OpCodes.Brtrue) + 1;
            int endIndex = codes.FindIndex(x => x.operand == (object)Method(typeof(InputActionRebindingExtensions), nameof(InputActionRebindingExtensions.LoadBindingOverridesFromJson), [typeof(IInputActionCollection2), typeof(string), typeof(bool)])) + 5;

            for (var i = startIndex; i <= endIndex; i++)
            {
                codes[i].opcode = OpCodes.Nop;
                codes[i].operand = null;
            }

            return codes.AsEnumerable();
        }
    }
}
