using GameNetcodeStuff;
using HarmonyLib;
using LCVR.Input;
using LCVR.Player;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using static HarmonyLib.AccessTools;

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

        var pivotAmount = Actions.Instance["Pivot"].ReadValue<Vector2>().x * 150;
        __instance.ghostObject.eulerAngles = new Vector3(__instance.ghostObject.eulerAngles.x,
            __instance.ghostObject.eulerAngles.y + Time.deltaTime * pivotAmount * 1f,
            __instance.ghostObject.eulerAngles.z);
    }
    
    /// <summary>
    /// Make the entering of build mode check the target structure from the hand, instead of from the camera
    /// </summary>
    [HarmonyPatch(typeof(ShipBuildModeManager), nameof(ShipBuildModeManager.EnterBuildMode))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> ShipBuilderEnterFromHand(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions);

        for (var i = 0; i < 2; i++)
        {
            matcher.MatchForward(false,
                    new CodeMatch(OpCodes.Ldfld,
                        Field(typeof(PlayerControllerB), nameof(PlayerControllerB.gameplayCamera))))
                .Advance(-2)
                .SetOpcodeAndAdvance(OpCodes.Nop)
                .RemoveInstructions(3)
                .InsertAndAdvance(
                    new CodeInstruction(OpCodes.Call, PropertyGetter(typeof(VRSession), nameof(VRSession.Instance))),
                    new CodeInstruction(OpCodes.Callvirt,
                        PropertyGetter(typeof(VRSession), nameof(VRSession.LocalPlayer))),
                    new CodeInstruction(OpCodes.Callvirt,
                        PropertyGetter(typeof(VRPlayer), nameof(VRPlayer.PrimaryController))),
                    new CodeInstruction(OpCodes.Callvirt,
                        PropertyGetter(typeof(VRController), nameof(VRController.InteractOrigin)))
                );
        }
        
        return matcher.InstructionEnumeration();
    }

    /// <summary>
    /// Make the placement of objects follow the hand rotation instead of the head rotation
    /// </summary>
    [HarmonyPatch(typeof(ShipBuildModeManager), nameof(ShipBuildModeManager.Update))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> ShipBuilderRayFromHand(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchForward(true,
            [
                new CodeMatch(OpCodes.Stfld, Field(typeof(ShipBuildModeManager), nameof(ShipBuildModeManager.playerCameraRay))),
            ])
            .Advance(1)
            .Insert(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, ((Action<ShipBuildModeManager>)UpdateRay).Method)
            )
            .InstructionEnumeration();
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
