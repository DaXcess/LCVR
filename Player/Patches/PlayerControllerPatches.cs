using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace LethalCompanyVR.Player.Patches
{
    // TODO: Determine what to do with dynamic FOV updates and stuff
    // (PlayerControllerB::Update)

    [HarmonyPatch(typeof(PlayerControllerB), "Update")]
    public static class PlayerControllerB_Update_Patch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            // Remove FOV updating
            for (int i = 302; i <= 313; i++)
            {
                codes[i].opcode = OpCodes.Nop;
                codes[i].operand = null;
            }

            return codes.AsEnumerable();
        }
    }
}
