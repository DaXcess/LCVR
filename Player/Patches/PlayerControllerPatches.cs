using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace LethalCompanyVR
{
    // TODO: Determine what to do with dynamic FOV updates and stuff
    // (PlayerControllerB::Update)

    // TODO: Try to see if this can be made Il2Cpp compatible

    [HarmonyPatch(typeof(PlayerControllerB), "Update")]
    public static class PlayerControllerB_Update_Patch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            if (Plugin.VR_ENABLED)
            {
                // Remove HUD rotating
                for (int i = 108; i <= 120; i++)
                {
                    codes[i].opcode = OpCodes.Nop;
                    codes[i].operand = null;
                }

                // Remove FOV updating
                for (int i = 302; i <= 313; i++)
                {
                    codes[i].opcode = OpCodes.Nop;
                    codes[i].operand = null;
                }
            }

            return codes.AsEnumerable();
        }
    }
}
