using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using GameNetcodeStuff;
using HarmonyLib;
using LCVR.Managers;
using UnityEngine;

using static HarmonyLib.AccessTools;

namespace LCVR.Patches.Spectating;

/// <summary>
/// Generic patches for the free roam spectator functionality
/// </summary>
[LCVRPatch]
[HarmonyPatch]
internal static class SpectatorPlayerPatches
{
    /// <summary>
    /// Set up some stuff before we actually die
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.KillPlayer))]
    [HarmonyPrefix]
    private static void BeforePlayerDeath(PlayerControllerB __instance)
    {
        if (!__instance.IsOwner)
            return;

        VRSession.Instance.SpectateManager.PlayerDeathInit();
    }

    /// <summary>
    /// We died
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.KillPlayer))]
    [HarmonyPostfix]
    private static void OnPlayerDeath(PlayerControllerB __instance)
    {
        if (!__instance.IsOwner)
            return;

        VRSession.Instance.SpectateManager.PlayerDeath();
    }

    /// <summary>
    /// Quick fix for when you are not the host, and some fields are set after `KillPlayer` has already executed
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.KillPlayerClientRpc))]
    [HarmonyPostfix]
    private static void KillPlayerClientRpc(PlayerControllerB __instance, int playerId)
    {
        if (playerId != (int)StartOfRound.Instance.localPlayerController.playerClientId)
            return;

        __instance.isPlayerControlled = true;
        __instance.thisPlayerModelArms.enabled = true;
    }

    /// <summary>
    /// If we were dead, perform necessary actions to recover properly
    /// </summary>
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ReviveDeadPlayers))]
    [HarmonyPrefix]
    private static void OnPlayerRevived()
    {
        VRSession.Instance.SpectateManager.PlayerRevive();
    }

    /// <summary>
    /// Prevent the game from spectating a player, since we use our own logic for this
    /// </summary>
    /// <returns></returns>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.SpectateNextPlayer))]
    [HarmonyPrefix]
    private static bool PreventSpectateNextPlayer()
    {
        return false;
    }

    /// <summary>
    /// Enable night vision lights when in factory and when dead
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.SetNightVisionEnabled))]
    [HarmonyPrefix]
    private static bool SetNightVisionEnabled(PlayerControllerB __instance)
    {
        if (__instance != StartOfRound.Instance.localPlayerController || !__instance.isPlayerDead)
            return true;

        __instance.nightVision.enabled = __instance.isInsideFactory;
        return false;
    }

    /// <summary>
    /// Force non-dead players to disable spatial audio if we're dead **and** we enabled global audio
    /// </summary>
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.UpdatePlayerVoiceEffects))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> SpectatorGlobalAudioPatches(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchForward(false,
                new CodeMatch(OpCodes.Callvirt,
                    PropertySetter(typeof(AudioLowPassFilter), nameof(AudioLowPassFilter.lowpassResonanceQ))))
            .Advance(-22)
            .RemoveInstructions(2)
            .Advance(2)
            .Insert(
                new CodeInstruction(OpCodes.Ldloc_1),
                new CodeInstruction(OpCodes.Call,
                    ((Action<AudioSource, PlayerVoiceIngameSettings, PlayerControllerB>)SetSpatialSettings).Method),
                new CodeInstruction(OpCodes.Ldloc_1)
            )
            .InstructionEnumeration();

        static void SetSpatialSettings(AudioSource audioSource, PlayerVoiceIngameSettings settings,
            PlayerControllerB player)
        {
            var isGlobal = VRSession.Instance?.SpectateManager?.GlobalAudio ?? false;
            var isSpectated = VRSession.Instance?.SpectateManager?.SpectatedPlayer == player;

            isGlobal = isGlobal || isSpectated;

            audioSource.spatialBlend = isGlobal ? 0 : 1;
            settings.set2D = isGlobal;
            audioSource.GetComponent<AudioLowPassFilter>().enabled = !isGlobal;
        }
    }
}