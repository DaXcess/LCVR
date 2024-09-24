using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using CullFactory.Data;
using CullFactory.Extenders;
using GameNetcodeStuff;
using HarmonyLib;
using LCVR.Patches;
using LCVR.Player;
using UnityEngine;
using static HarmonyLib.AccessTools;

namespace LCVR.Compatibility.CullFactory;

[LCVRPatch(dependency: Compat.CullFactory)]
[HarmonyPatch]
internal static class Patches
{
    /// <summary>
    /// Fix for CullFactory to include the VR helmet lights in the <see cref="DynamicObjects.allPlayerLights"/> array
    /// </summary>
    [HarmonyPatch(typeof(DynamicObjects), nameof(DynamicObjects.CollectAllPlayerLights))]
    [HarmonyPostfix]
    private static void OnCollectAllPlayerLights()
    {
        if (!VRSession.Instance)
            return;
        
        var clientId = VRSession.Instance.LocalPlayer.PlayerController.playerClientId;
        var lights = DynamicObjects.allPlayerLights[clientId];
        var cameraLights = VRSession.Instance.MainCamera.GetComponentsInChildren<Light>();
        
        DynamicObjects.allPlayerLights[clientId] = [..lights, ..cameraLights];
    }

    /// <summary>
    /// Rather interesting patch that delays the player teleport code in CullFactory by a single frame
    ///
    /// Fixes items turning invisible due to CullFactory thinking they're inside/outside the factory whilst we are not
    /// </summary>
    [HarmonyPatch(typeof(TeleportExtender), nameof(TeleportExtender.OnPlayerTeleported))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> DelayPlayerTeleport(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchForward(false, new CodeMatch(OpCodes.Call,
                Method(typeof(DynamicObjects), nameof(DynamicObjects.OnPlayerTeleported))))
            .SetOperandAndAdvance(((Action<PlayerControllerB>)OnPlayerTeleport).Method)
            .InstructionEnumeration();

        static void OnPlayerTeleport(PlayerControllerB player)
        {
            StartOfRound.Instance.StartCoroutine(OnPlayerTeleportedCoroutine(player));
        }

        static IEnumerator OnPlayerTeleportedCoroutine(PlayerControllerB player)
        {
            yield return null;
            
            DynamicObjects.OnPlayerTeleported(player);
        }
    }
}
