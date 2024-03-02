using HarmonyLib;
using LCVR.Input;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using LCVR.Assets;
using UnityEngine.InputSystem;
using static HarmonyLib.AccessTools;

namespace LCVR.Patches;

[LCVRPatch]
[HarmonyPatch]
public class InputPatches
{
    /// <summary>
    /// Reload the bindings when the in-game player settings get loaded
    /// </summary>
    [HarmonyPatch(typeof(IngamePlayerSettings), nameof(IngamePlayerSettings.LoadSettingsFromPrefs))]
    [HarmonyPostfix]
    private static void OnLoadSettings()
    {
        Actions.Instance.Reload();
    }

    /// <summary>
    /// Disable Lethal Company's legacy inputs
    /// </summary>
    [HarmonyPatch(typeof(PlayerActions), MethodType.Constructor)]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchForward(false, new CodeMatch(OpCodes.Ldstr))
            .SetOperandAndAdvance(AssetManager.nullInputActions.ToJson())
            .InstructionEnumeration();
    }
}

[LCVRPatch]
[HarmonyPatch(typeof(IngamePlayerSettings), nameof(IngamePlayerSettings.DiscardChangedSettings))]
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
