using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.InputSystem;

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

    [HarmonyPatch(typeof(PlayerControllerB), "SwitchItem_performed")]
    public static class PlayerControllerB_SwitchItem_Patch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            var codes = new List<CodeInstruction>(instructions);

            if (Plugin.VR_ENABLED)
            {
                var readValueMethod = typeof(InputAction.CallbackContext).GetMethod("ReadValue", []).MakeGenericMethod(typeof(float));
                var mathAbsMethod = typeof(Math).GetMethod("Abs", [typeof(float)]);

                var jumpTarget = codes[0];
                var jumpLabel = generator.DefineLabel();

                codes[0].labels.Add(jumpLabel);

                // This little POS here was supposed to be 1 even though there's only 1 method param?? What is going on?
                codes.Insert(0, new CodeInstruction(OpCodes.Ldarga_S, 1));
                codes.Insert(1, new CodeInstruction(OpCodes.Call, readValueMethod));
                codes.Insert(2, new CodeInstruction(OpCodes.Call, mathAbsMethod));
                codes.Insert(3, new CodeInstruction(OpCodes.Ldc_R4, 0.75f));
                codes.Insert(4, new CodeInstruction(OpCodes.Bge_Un, jumpLabel));
                codes.Insert(5, new CodeInstruction(OpCodes.Ret));
            }

            return codes.AsEnumerable();
        }
    }

    [HarmonyPatch]
    public class BasicPlayerControllerPatches
    {
        [HarmonyPatch(typeof(PlayerControllerB), "PlayerLookInput")]
        [HarmonyPostfix]
        private static void AfterPlayerLookInput(PlayerControllerB __instance)
        {
            var rot = Actions.XR_HeadRotation.ReadValue<Quaternion>().eulerAngles.x;

            if (rot > 180)
            {
                rot -= 360;
            }

            typeof(PlayerControllerB).GetField("cameraUp", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(__instance, rot);
        }
    }
}
