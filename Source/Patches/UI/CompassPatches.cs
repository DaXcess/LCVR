using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using LCVR.Managers;
using UnityEngine;

using static HarmonyLib.AccessTools;

namespace LCVR.Patches.UI;

[LCVRPatch]
[HarmonyPatch]
internal static class CompassPatches
{
    /// <summary>
    /// "Rotate" the HUD compass based on your hand's rotation instead of your player's rotation
    /// </summary>
    [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.Update))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> CompassRotateBasedOnHand(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchForward(false,
                new CodeMatch(OpCodes.Ldfld, Field(typeof(HUDManager), nameof(HUDManager.compassImage))))
            .Advance(1)
            .RemoveInstructions(7)
            .InsertAndAdvance(new CodeInstruction(OpCodes.Call, ((Func<float>)GetCurrentRotation).Method))
            .InstructionEnumeration();
        
        static float GetCurrentRotation()
        {
            if (VRSession.Instance is not { LocalPlayer: {} player } session)
                return GameNetworkManager.Instance.localPlayerController.transform.eulerAngles.y / 360;

            if (session.HUD.isHandUiDisabled)
                return VRSession.Instance.MainCamera.transform.eulerAngles.y / 360;
                
            var origin = player.PrimaryController.InteractOrigin;
            var fwd = Vector3.Dot(origin.forward, Vector3.up) > 0.7f ? -origin.right : origin.forward;
            var normalized = new Vector3(fwd.x, 0, fwd.z).normalized;
            var angle = Mathf.Atan2(normalized.x, normalized.z) * Mathf.Rad2Deg;

            return (angle < 0 ? angle + 360 : angle) / 360;
        }
    }
}
