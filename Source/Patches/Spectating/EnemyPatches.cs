using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Prevent a nutcracker from keeping aggro on a spectator after killing them
    /// </summary>
    [HarmonyPatch(typeof(NutcrackerEnemyAI), nameof(NutcrackerEnemyAI.Update))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> NutcrackerLoseAggroPatch(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchForward(false,
                new CodeMatch(OpCodes.Call, Method(typeof(EnemyAI), nameof(EnemyAI.CheckLineOfSightForPosition))))
            .Set(OpCodes.Call,
                ((Func<EnemyAI, Vector3, float, int, float, Transform, int, bool>)CheckLineOfSightAlt).Method)
            .Insert(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld,
                    Field(typeof(NutcrackerEnemyAI), nameof(NutcrackerEnemyAI.lastPlayerSeenMoving)))
            )
            .InstructionEnumeration();

        static bool CheckLineOfSightAlt(EnemyAI enemy, Vector3 position, float width, int range,
            float proximityAwareness, Transform overrideEye, int playerId)
        {
            if ((uint)playerId == StartOfRound.Instance.localPlayerController.playerClientId &&
                StartOfRound.Instance.localPlayerController.isPlayerDead)
                return false;

            return enemy.CheckLineOfSightForPosition(position, width, range, proximityAwareness, overrideEye);
        }
    }

    /// <summary>
    /// Prevent detection by centipedes that are hidden on the ceiling
    /// </summary>
    [HarmonyPatch(typeof(CentipedeAI), nameof(CentipedeAI.TriggerCentipedeFallServerRpc))]
    [HarmonyPrefix]
    private static bool TriggerCentipedeFall(CentipedeAI __instance)
    {
        var networkManager = __instance.NetworkManager;

        if ((int)__instance.__rpc_exec_stage != 1 && (networkManager.IsClient || networkManager.IsHost) &&
            StartOfRound.Instance.localPlayerController.isPlayerDead)
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
    private static IEnumerable<CodeInstruction> DressGirlTargetDeadPlayerFix(IEnumerable<CodeInstruction> instructions,
        ILGenerator generator)
    {
        return new CodeMatcher(instructions, generator)
            .MatchForward(false,
                new CodeMatch(OpCodes.Call,
                    PropertyGetter(typeof(NetworkBehaviour), nameof(NetworkBehaviour.IsServer))))
            .Advance(6)
            .CreateLabel(out var label)
            .Advance(-1)
            .SetInstructionAndAdvance(new CodeInstruction(OpCodes.Brfalse, label))
            .InsertAndAdvance([
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, Field(typeof(DressGirlAI), nameof(DressGirlAI.hauntingPlayer))),
                new CodeInstruction(OpCodes.Ldfld,
                    Field(typeof(PlayerControllerB), nameof(PlayerControllerB.isPlayerDead))),
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

    /// <summary>
    /// Make the locust bees ignore dead players
    /// </summary>
    [HarmonyPatch(typeof(DocileLocustBeesAI), nameof(DocileLocustBeesAI.DoAIInterval))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> LocustBeesIgnoreDeadPlayers(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchForward(false,
                new CodeMatch(OpCodes.Call,
                    Method(typeof(UnityEngine.Physics), nameof(UnityEngine.Physics.CheckSphere),
                        [typeof(Vector3), typeof(float), typeof(int), typeof(QueryTriggerInteraction)])))
            .SetOperandAndAdvance(((Func<Vector3, float, int, QueryTriggerInteraction, bool>)CheckCollision).Method)
            .InstructionEnumeration();

        static bool CheckCollision(Vector3 position, float radius, int layerMask,
            QueryTriggerInteraction queryTriggerInteraction)
        {
            var colliders = new Collider[8];
            var size = UnityEngine.Physics.OverlapSphereNonAlloc(position, radius, colliders, layerMask,
                queryTriggerInteraction);

            var everyoneDead =
                colliders[..size].All(collider => collider.GetComponent<PlayerControllerB>()?.isPlayerDead ?? false);

            return !everyoneDead;
        }
    }

    /// <summary>
    /// Fix stingrays triggering when dead player walks over them
    /// </summary>
    [HarmonyPatch(typeof(StingrayAI), nameof(StingrayAI.Update))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> StingrayIgnoreDeadPlayer(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            // Prevent spectators from being squirted on
            .MatchForward(false, new CodeMatch(OpCodes.Ldfld, Field(typeof(PlayerControllerB), nameof(PlayerControllerB.gameplayCamera))))
            .MatchBack(false, new CodeMatch(OpCodes.Ldfld, Field(typeof(GameNetworkManager), nameof(GameNetworkManager.localPlayerController))))
            .Advance(-1)
            .RemoveInstructions(3)
            .Insert(
                new CodeInstruction(OpCodes.Call, ((Func<PlayerControllerB, bool>)IsLocalAndAlive).Method)
            )
            // Detect local player step on
            .MatchForward(false, new CodeMatch(OpCodes.Callvirt, PropertyGetter(typeof(CharacterController), nameof(CharacterController.isGrounded))))
            .Set(OpCodes.Call, ((Func<PlayerControllerB, bool>)IsGroundedAndAlive).Method)
            .Advance(-1)
            .RemoveInstruction()
            .InstructionEnumeration();

        static bool IsGroundedAndAlive(PlayerControllerB player) =>
            player.thisController.isGrounded && !player.isPlayerDead;

        static bool IsLocalAndAlive(PlayerControllerB player) =>
            player == GameNetworkManager.Instance.localPlayerController && !player.isPlayerDead;
    }
}