using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine.InputSystem;

namespace LCVR.Patches
{
    [LCVRPatch]
    [HarmonyPatch]
    internal static class HUDManagerPatches
    {
        /// <summary>
        /// Disables the ping scan if you are in the pause menu
        /// </summary>
        [HarmonyPatch(typeof(HUDManager), "CanPlayerScan")]
        [HarmonyPrefix]
        private static bool CanPlayerScan(ref bool __result)
        {
            if (GameNetworkManager.Instance.localPlayerController.quickMenuManager.isMenuOpen)
            {
                __result = false;
                return false;
            }

            return true;
        }
    }

    [LCVRPatch(LCVRPatchTarget.Universal)]
    [HarmonyPatch(typeof(HUDManager), "Update")]
    internal static class HUDManagerLeaveEarlyPatches
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = new List<CodeInstruction>(instructions);
            var startIndex = codes.FindIndex(instruction => instruction.opcode == OpCodes.Callvirt && (MethodInfo)instruction.operand == AccessTools.PropertyGetter(typeof(PlayerActions), "Movement")) - 2;

            var labels = codes[startIndex].labels;
            codes[startIndex++] = new(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(IngamePlayerSettings), nameof(IngamePlayerSettings.Instance)))
            {
                labels = labels
            };

            codes[startIndex++] = new(OpCodes.Ldfld, AccessTools.Field(typeof(IngamePlayerSettings), nameof(IngamePlayerSettings.playerInput)));
            codes[startIndex++] = new(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(PlayerInput), nameof(PlayerInput.actions)));
            codes[startIndex++] = new(OpCodes.Ldstr, "PingScan");
            codes[startIndex++] = new(OpCodes.Ldc_I4_0);
            codes[startIndex++] = new(OpCodes.Callvirt, AccessTools.Method(typeof(InputActionAsset), nameof(InputActionAsset.FindAction), [typeof(string), typeof(bool)]));
            codes[startIndex++] = new(OpCodes.Callvirt, AccessTools.Method(typeof(InputAction), nameof(InputAction.IsPressed)));

            return codes.AsEnumerable();
        }
    }
}
