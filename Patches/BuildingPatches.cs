using GameNetcodeStuff;
using HarmonyLib;
using LCVR.Input;
using LCVR.Player;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LCVR.Patches
{
    [LCVRPatch]
    [HarmonyPatch]
    internal static class ShipBuildModeManagerPatches
    {
        private static InputAction pivotAction;

        [HarmonyPatch(typeof(ShipBuildModeManager), "OnEnable")]
        [HarmonyPostfix]
        private static void OnStart()
        {
            pivotAction = Actions.FindAction("Pivot");
        }

        [HarmonyPatch(typeof(ShipBuildModeManager), "Update")]
        [HarmonyPostfix]
        private static void OnUpdate(ShipBuildModeManager __instance)
        {
            if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null)
                return;

            if (!__instance.InBuildMode)
                return;

            var pivotAmount = pivotAction.ReadValue<Vector2>().x * 150;
            __instance.ghostObject.eulerAngles = new Vector3(__instance.ghostObject.eulerAngles.x, __instance.ghostObject.eulerAngles.y + Time.deltaTime * pivotAmount * 1f, __instance.ghostObject.eulerAngles.z);
        }
    }

    [LCVRPatch]
    [HarmonyPatch(typeof(ShipBuildModeManager), "Update")]
    internal static class ShipBuildModeManagerFromHandPatches
    {
        private static readonly FieldInfo playerCameraRay = AccessTools.Field(typeof(ShipBuildModeManager), "playerCameraRay");

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                yield return instruction;
            
                if (instruction.opcode == OpCodes.Stfld && (FieldInfo)instruction.operand == playerCameraRay)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ShipBuildModeManagerFromHandPatches), "UpdateRay"));
                }
            }
        }

        public static void UpdateRay(ShipBuildModeManager manager)
        {
            var origin = StartOfRound.Instance.localPlayerController.GetComponent<VRPlayer>().mainHand.interactOrigin;
            var ray = new Ray(origin.position, origin.forward);

            AccessTools.Field(typeof(ShipBuildModeManager), "playerCameraRay").SetValue(manager, ray);
        }
    }

    [LCVRPatch]
    [HarmonyPatch]
    internal static class PlayerControllerBuildingPatches
    {
        [HarmonyPatch(typeof(PlayerControllerB), "Discard_performed")]
        [HarmonyPrefix]
        private static bool DiscardPrefix()
        {
            return !ShipBuildModeManager.Instance.InBuildMode;
        }

        [HarmonyPatch(typeof(PlayerControllerB), "ItemSecondaryUse_performed")]
        [HarmonyPrefix]
        private static bool SecondaryUsePrefix()
        {
            return !ShipBuildModeManager.Instance.InBuildMode;
        }

        [HarmonyPatch(typeof(PlayerControllerB), "ItemTertiaryUse_performed")]
        [HarmonyPrefix]
        private static bool TertiaryUsePrefix()
        {
            return !ShipBuildModeManager.Instance.InBuildMode;
        }
    }
}
