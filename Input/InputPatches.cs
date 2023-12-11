using HarmonyLib;
using System.Collections.Generic;
using System.Linq;

namespace LethalCompanyVR
{
    // TODO: Is this even necessary anymore?

    [HarmonyPatch(typeof(PlayerActions))]
    [HarmonyPatch(MethodType.Constructor)]
    public class InputPatches
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            codes[6].operand = Properties.Resources.inputs;
            
            return codes.AsEnumerable();
        }
    }
}
