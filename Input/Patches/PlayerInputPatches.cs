using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LethalCompanyVR
{
    // TODO: Try to make Il2Cpp compatible alternative

    [HarmonyPatch(typeof(PlayerActions))]
    [HarmonyPatch(MethodType.Constructor)]
    public static class PlayerInputPatches
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            if (Plugin.VR_ENABLED)
            {
                codes[6].operand = Encoding.UTF8.GetString(Properties.Resources.inputs);
            }

            return codes.AsEnumerable();
        }
    }
}
