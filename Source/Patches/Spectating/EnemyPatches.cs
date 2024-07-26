using System.Collections.Generic;
using System.Reflection.Emit;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using static HarmonyLib.AccessTools;

namespace LCVR.Patches.Spectating;

/// <summary>
/// Enemy specific patches for the free roam spectator functionality
/// </summary>
[LCVRPatch]
[HarmonyPatch]
internal static class SpectatorEnemyPatches
{
    /// <summary>
    /// Prevent the nutcracker from seeing the local player if they're dead
    /// </summary>
    [HarmonyPatch(typeof(NutcrackerEnemyAI), nameof(NutcrackerEnemyAI.CheckLineOfSightForLocalPlayer))]
    [HarmonyPrefix]
    private static bool NutcrackerCheckForLocalPlayer(ref bool __result)
    {
        if (!StartOfRound.Instance.localPlayerController.isPlayerDead)
        {
            return true;
        }

        __result = false;
        return false;
    }

    /// <summary>
    /// Prevent detection by centipedes that are hidden on the ceiling
    /// </summary>
    [HarmonyPatch(typeof(CentipedeAI), nameof(CentipedeAI.TriggerCentipedeFallServerRpc))]
    [HarmonyPrefix]
    private static bool TriggerCentipedeFall(CentipedeAI __instance)
    {
        var networkManager = __instance.NetworkManager;

        if ((int)__instance.__rpc_exec_stage != 1 && (networkManager.IsClient || networkManager.IsHost) && StartOfRound.Instance.localPlayerController.isPlayerDead)
        {
            __instance.triggeredFall = false;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Fix for the dress girl AI not swapping players if the host is dead and being haunted
    /// </summary>
    [HarmonyPatch(typeof(DressGirlAI), nameof(DressGirlAI.Update))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> DressGirlTargetDeadPlayerFix(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        return new CodeMatcher(instructions, generator)
            .MatchForward(false, new CodeMatch(OpCodes.Call, PropertyGetter(typeof(NetworkBehaviour), nameof(NetworkBehaviour.IsServer))))
            .Advance(6)
            .CreateLabel(out var label)
            .Advance(-1)
            .SetInstructionAndAdvance(new(OpCodes.Brfalse, label))
            .InsertAndAdvance([
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, Field(typeof(DressGirlAI), nameof(DressGirlAI.hauntingPlayer))),
                new(OpCodes.Ldfld, Field(typeof(PlayerControllerB), nameof(PlayerControllerB.isPlayerDead))),
            ])
            .InsertBranch(OpCodes.Brfalse, 21)
            .InstructionEnumeration();
    }

    /// <summary>
    /// Fix for the Old Bird enemies to not try to grab dead players
    /// </summary>
    [HarmonyPatch(typeof(RadMechAI), nameof(RadMechAI.AttemptGrabIfClose))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> RadMechDontGrabDeadPlayers(IEnumerable<CodeInstruction> instructions,
        ILGenerator generator)
    {
        return new CodeMatcher(instructions, generator)
            .MatchForward(false,
            [
                new CodeMatch(OpCodes.Ldfld,
                    Field(typeof(PlayerControllerB), nameof(PlayerControllerB.isPlayerControlled)))
            ])
            .Advance(2)
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Call,
                    PropertyGetter(typeof(StartOfRound), nameof(StartOfRound.Instance))),
                new CodeInstruction(OpCodes.Ldfld,
                    Field(typeof(StartOfRound), nameof(StartOfRound.allPlayerScripts))),
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Ldelem_Ref),
                new CodeInstruction(OpCodes.Ldfld,
                    Field(typeof(PlayerControllerB), nameof(PlayerControllerB.isPlayerDead)))
            )
            .InsertBranchAndAdvance(OpCodes.Brtrue, 78)
            .InstructionEnumeration();
    }

    private static readonly int AlphaCutoff = Shader.PropertyToID("_AlphaCutoff");
    
    /// <summary>
    /// Make the clay surgeon always visible if we're dead
    /// </summary>
    [HarmonyPatch(typeof(ClaySurgeonAI), nameof(ClaySurgeonAI.SetVisibility))]
    [HarmonyPostfix]
    private static void ClaySurgeonVisibleWhenDead(ClaySurgeonAI __instance)
    {
        if (StartOfRound.Instance.localPlayerController.isPlayerDead)
            __instance.thisMaterial.SetFloat(AlphaCutoff, 0);
    }
}