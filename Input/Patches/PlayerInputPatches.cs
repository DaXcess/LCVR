using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LethalCompanyVR.Input.Patches
{
    [HarmonyPatch(typeof(PlayerActions))]
    [HarmonyPatch(MethodType.Constructor)]
    public static class PlayerInputPatches
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            codes[6].operand = Encoding.UTF8.GetString(Properties.Resources.inputs);

            return codes.AsEnumerable();
        }
    }
}
