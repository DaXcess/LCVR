using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using GameNetcodeStuff;
using HarmonyLib;
using LCVR.Patches;
using UnityEngine;

using static HarmonyLib.AccessTools;

namespace LCVR.Player.Spectating;

/// <summary>
/// Generic AI patches for the free roam spectator functionality
/// </summary>
[LCVRPatch]
[HarmonyPatch]
internal static class SpectatorAIPatches
{
    /// <summary>
    /// Prevent dead player from notifying enemies that they are being looked at
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.HasLineOfSightToPosition))]
    [HarmonyPrefix]
    private static bool CanCheckLineOfSight(PlayerControllerB __instance, ref bool __result)
    {
        if (!__instance.IsOwner || !__instance.isPlayerControlled || !__instance.isPlayerDead)
            return true;

        __result = false;
        return false;
    }

    /// <summary>
    /// Prevent dead player from making audible noises for enemies to hear
    /// </summary>
    [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.PlayAudibleNoise))]
    [HarmonyPrefix]
    private static bool CanBeHeard()
    {
        return !StartOfRound.Instance.localPlayerController.isPlayerDead;
    }

    /// <summary>
    /// Prevent player line of sight detection for dead players
    /// </summary>
    [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.CheckLineOfSightForPlayer))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> LineOfSightPlayerIgnoreDeadPlayer(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        return new CodeMatcher(instructions, generator)
            .MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Ldfld && (FieldInfo)i.operand == Field(typeof(StartOfRound), nameof(StartOfRound.allPlayerScripts))))
            .Advance(-1)
            .Insert(new CodeInstruction(OpCodes.Call, PropertyGetter(typeof(StartOfRound), nameof(StartOfRound.Instance))))
            .CreateLabel(out var label)
            .Advance(1)
            .InsertAndAdvance(new CodeInstruction(OpCodes.Ldfld, Field(typeof(StartOfRound), nameof(StartOfRound.allPlayerScripts))))
            .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_1))
            .InsertAndAdvance(new CodeInstruction(OpCodes.Ldelem_Ref))
            .InsertAndAdvance(new CodeInstruction(OpCodes.Ldfld, Field(typeof(PlayerControllerB), nameof(PlayerControllerB.isPlayerDead))))
            .InsertBranchAndAdvance(OpCodes.Brtrue, 77)
            .MatchForward(false, new CodeMatch(OpCodes.Blt))
            .Advance(1)
            .MatchForward(false, new CodeMatch(OpCodes.Blt))
            .SetOperandAndAdvance(label)
            .InstructionEnumeration();
    }

    /// <summary>
    /// Prevent closest player line of sight detection for dead players
    /// </summary>
    [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.CheckLineOfSightForClosestPlayer))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> LineOfSightClosestPlayerIgnoreDeadPlayer(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        return new CodeMatcher(instructions, generator)
            .MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Ldfld && (FieldInfo)i.operand == Field(typeof(StartOfRound), nameof(StartOfRound.allPlayerScripts))))
            .Advance(-1)
            .Insert(new CodeInstruction(OpCodes.Call, PropertyGetter(typeof(StartOfRound), nameof(StartOfRound.Instance))))
            .CreateLabel(out var label)
            .Advance(1)
            .InsertAndAdvance(new CodeInstruction(OpCodes.Ldfld, Field(typeof(StartOfRound), nameof(StartOfRound.allPlayerScripts))))
            .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, 4))
            .InsertAndAdvance(new CodeInstruction(OpCodes.Ldelem_Ref))
            .InsertAndAdvance(new CodeInstruction(OpCodes.Ldfld, Field(typeof(PlayerControllerB), nameof(PlayerControllerB.isPlayerDead))))
            .InsertBranchAndAdvance(OpCodes.Brtrue, 79)
            .MatchForward(false, new CodeMatch(OpCodes.Blt))
            .Advance(1)
            .MatchForward(false, new CodeMatch(OpCodes.Blt))
            .SetOperandAndAdvance(label)
            .InstructionEnumeration();
    }

    /// <summary>
    /// Prevent collision detection on dead players
    /// </summary>
    [HarmonyPatch(typeof(EnemyAICollisionDetect), "OnTriggerStay")]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> OnCollidePlayerTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        return new CodeMatcher(instructions, generator)
            .MatchForward(false, new CodeMatch(OpCodes.Brfalse))
            .Advance(1)
            .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_1))
            .InsertAndAdvance(new CodeInstruction(OpCodes.Callvirt, PropertyGetter(typeof(Component), nameof(Component.gameObject))))
            .InsertAndAdvance(new CodeInstruction(OpCodes.Callvirt, Method(typeof(GameObject), nameof(GameObject.GetComponent)).MakeGenericMethod([typeof(PlayerControllerB)])))
            .InsertAndAdvance(new CodeInstruction(OpCodes.Ldfld, Field(typeof(PlayerControllerB), nameof(PlayerControllerB.isPlayerDead))))
            .InsertBranchAndAdvance(OpCodes.Brfalse_S, 8)
            .InsertAndAdvance(new CodeInstruction(OpCodes.Ret))
            .InstructionEnumeration();
    }
}