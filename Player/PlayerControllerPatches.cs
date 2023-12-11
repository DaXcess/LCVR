using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace LethalCompanyVR
{
    [HarmonyPatch(typeof(PlayerControllerB), "Update")]
    public static class PlayerControllerB_Update_Patch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            // Remove HUD rotating
            for (int i = 111; i <= 123; i++)
            {
                codes[i].opcode = OpCodes.Nop;
                codes[i].operand = null;
            }

            // Remove FOV updating
            for (int i = 305; i <= 316; i++)
            {
                codes[i].opcode = OpCodes.Nop;
                codes[i].operand = null;
            }

            // Remove Player Rig Updating
            //for (int i = 1965; i <= 1990; i++)
            //{
            //    codes[i].opcode = OpCodes.Nop;
            //    codes[i].operand = null;
            //}

            return codes.AsEnumerable();
        }
    }

    [HarmonyPatch(typeof(PlayerControllerB), "LateUpdate")]
    internal static class PlayerControllerB_LateUpdate_Patches
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            // Remove Player Rig Updating
            //for (int i = 497; i <= 516; i++)
            //{
            //    codes[i].opcode = OpCodes.Nop;
            //    codes[i].operand = null;
            //}

            return codes.AsEnumerable();
        }
    }

    [HarmonyPatch(typeof(PlayerControllerB), "ScrollMouse_performed")]
    public static class PlayerControllerB_SwitchItem_Patch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            var codes = new List<CodeInstruction>(instructions);

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

            return codes.AsEnumerable();
        }
    }

    [HarmonyPatch]
    public static class PlayerControllerPatches
    {
        // TODO: Somehow get the animator to work properly in VR
        [HarmonyPatch(typeof(PlayerControllerB), "Update")]
        [HarmonyPostfix]
        private static void UpdatePrefix(PlayerControllerB __instance)
        {
            if (__instance.playerBodyAnimator.runtimeAnimatorController != null)
            {
                __instance.playerBodyAnimator.runtimeAnimatorController = null;
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB), "DamagePlayer")]
        [HarmonyPostfix]
        public static void AfterDamagePlayer()
        {
            VRPlayer.VibrateController(XRNode.LeftHand, 0.1f, 0.5f);
            VRPlayer.VibrateController(XRNode.RightHand, 0.1f, 0.5f);
        }
    }
}
