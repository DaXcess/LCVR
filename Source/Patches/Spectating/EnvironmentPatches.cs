using System.Collections;
using GameNetcodeStuff;
using HarmonyLib;
using LCVR.Patches;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace LCVR.Player.Spectating;

/// <summary>
/// Environment specific patches for the freeroam spectator feature
/// </summary>
[LCVRPatch]
[HarmonyPatch]
internal static class SpectatorEnvironmentPatches
{
    /// <summary>
    /// Prevent dead players from being affected by quicksand
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.CheckConditionsForSinkingInQuicksand))]
    [HarmonyPrefix]
    private static bool CanSinkInQuicksand(ref bool __result)
    {
        if (!StartOfRound.Instance.localPlayerController.isPlayerDead)
            return true;

        __result = false;
        return false;
    }

    /// <summary>
    /// Prevent dead players from colliding with spider webs
    /// </summary>
    [HarmonyPatch(typeof(SandSpiderWebTrap), nameof(SandSpiderWebTrap.OnTriggerStay))]
    [HarmonyPrefix]
    private static bool SandSpiderWebTrapOnCollide()
    {
        if (!StartOfRound.Instance || !StartOfRound.Instance.localPlayerController)
            return true;
        
        return !StartOfRound.Instance.localPlayerController.isPlayerDead;
    }

    /// <summary>
    /// Prevent dead players from triggering touch-based interact triggers (e.g. garage door on Experimentation)
    /// </summary>
    [HarmonyPatch(typeof(InteractTrigger), nameof(InteractTrigger.OnTriggerEnter))]
    [HarmonyPrefix]
    private static bool InteractTriggerOnCollide()
    {
        if (!StartOfRound.Instance || !StartOfRound.Instance.localPlayerController)
            return true;
        
        return !StartOfRound.Instance.localPlayerController.isPlayerDead;
    }

    /// <summary>
    /// Prevent dead players from collapsing the bridge
    /// </summary>
    [HarmonyPatch(typeof(BridgeTrigger), nameof(BridgeTrigger.OnTriggerStay))]
    [HarmonyPrefix]
    private static bool BridgeTriggerOnCollide()
    {
        if (!StartOfRound.Instance || !StartOfRound.Instance.localPlayerController)
            return true;
        
        return !StartOfRound.Instance.localPlayerController.isPlayerDead;
    }

    /// <summary>
    /// Prevent interact triggers from kicking the local player off a ladder when dead
    /// </summary>
    [HarmonyPatch(typeof(InteractTrigger), nameof(InteractTrigger.Update))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> InteractTriggerAllowLadderWhenDead(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchForward(true, new CodeMatch(OpCodes.Ret))
            .Advance(1)
            .SetOpcodeAndAdvance(OpCodes.Nop)
            .RemoveInstructions(13)
            .InstructionEnumeration();
    }

    /// <summary>
    /// Prevent dead players from making a noise when going through entrances
    /// </summary>
    [HarmonyPatch(typeof(EntranceTeleport), nameof(EntranceTeleport.TeleportPlayerServerRpc))]
    [HarmonyPrefix]
    private static bool SilenceDoorTeleport(EntranceTeleport __instance, int playerObj)
    {
        var networkManager = __instance.NetworkManager;

        var networkCheck = networkManager.IsClient || networkManager.IsHost;
        var localPlayerCheck = StartOfRound.Instance.allPlayerScripts[playerObj] == StartOfRound.Instance.localPlayerController;
        var localDeadCheck = StartOfRound.Instance.localPlayerController.isPlayerDead;

        return !networkCheck || !localPlayerCheck || !localDeadCheck;
    }

    /// <summary>
    /// Prevent dead players from being teleported if they don't have a body to teleport
    /// </summary>
    [HarmonyPatch(typeof(ShipTeleporter), nameof(ShipTeleporter.beamUpPlayer))]
    [HarmonyPrefix]
    private static bool PreventTeleportDeadPlayer(ShipTeleporter __instance, ref IEnumerator __result)
    {
        var target = StartOfRound.Instance.mapScreen.targetedPlayer;
        if (target != StartOfRound.Instance.localPlayerController || !target.isPlayerDead ||
            target.deadBody is not null)
            return true;
        
        __instance.shipTeleporterAudio.PlayOneShot(__instance.teleporterSpinSFX);
        __result = Utils.NopRoutine();
        return false;
    }
}
