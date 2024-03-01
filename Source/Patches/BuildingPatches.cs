using GameNetcodeStuff;
using HarmonyLib;
using LCVR.Input;
using LCVR.Player;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace LCVR.Patches;

[LCVRPatch]
[HarmonyPatch]
internal static class ShipBuildModeManagerPatches
{
    /// <summary>
    /// Make use of the Pivot input action to rotate objects while in build mode
    /// </summary>
    [HarmonyPatch(typeof(ShipBuildModeManager), nameof(ShipBuildModeManager.Update))]
    [HarmonyPostfix]
    private static void OnUpdate(ShipBuildModeManager __instance)
    {
        if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null)
            return;

        if (!__instance.InBuildMode)
            return;

        var pivotAmount = Actions.Instance["Controls/Pivot"].ReadValue<Vector2>().x * 150;
        __instance.ghostObject.eulerAngles = new Vector3(__instance.ghostObject.eulerAngles.x, __instance.ghostObject.eulerAngles.y + Time.deltaTime * pivotAmount * 1f, __instance.ghostObject.eulerAngles.z);
    }

    /// <summary>
    /// Make the placement of objects follow the hand rotation instead of the head rotation
    /// </summary>
    [HarmonyPatch(typeof(ShipBuildModeManager), nameof(ShipBuildModeManager.Update))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> ShipBuilderRayFromHand(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            yield return instruction;

            if (instruction.opcode == OpCodes.Stfld && (FieldInfo)instruction.operand == AccessTools.Field(typeof(ShipBuildModeManager), "playerCameraRay"))
            {
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Call, ((Action<ShipBuildModeManager>)UpdateRay).Method);
            }
        }
    }

    private static void UpdateRay(ShipBuildModeManager manager)
    {
        var origin = VRSession.Instance.LocalPlayer.PrimaryController.InteractOrigin;
        var ray = new Ray(origin.position, origin.forward);

        manager.playerCameraRay = ray;
    }
}

[LCVRPatch]
[HarmonyPatch]
internal static class PlayerControllerBuildingPatches
{
    /// <summary>
    /// Prevent dropping items while in build mode
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.Discard_performed))]
    [HarmonyPrefix]
    private static bool DiscardPrefix()
    {
        return !ShipBuildModeManager.Instance.InBuildMode;
    }

    /// <summary>
    /// Prevent using items while in build mode
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.ItemSecondaryUse_performed))]
    [HarmonyPrefix]
    private static bool SecondaryUsePrefix()
    {
        return !ShipBuildModeManager.Instance.InBuildMode;
    }

    /// <summary>
    /// Prevent using items while in build mode
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.ItemTertiaryUse_performed))]
    [HarmonyPrefix]
    private static bool TertiaryUsePrefix()
    {
        return !ShipBuildModeManager.Instance.InBuildMode;
    }
}
