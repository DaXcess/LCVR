using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using LCVR.Managers;

using static HarmonyLib.AccessTools;

namespace LCVR.Patches;

[LCVRPatch]
[HarmonyPatch]
internal static class EntranceTeleportPatches
{
    /// <summary>
    /// Force the player to face away from the entrance when entering the facility
    /// </summary>
    [HarmonyPatch(typeof(EntranceTeleport), nameof(EntranceTeleport.TeleportPlayer))]
    [HarmonyPostfix]
    private static void OnTeleportPlayer(EntranceTeleport __instance)
    {
        var entrancePoint = __instance.exitScript.entrancePoint;
        var rotation = entrancePoint.eulerAngles.y - VRSession.Instance.MainCamera.transform.parent.localEulerAngles.y;

        VRSession.Instance.LocalPlayer.TurningProvider.SetOffset(rotation);
    }

    /// <summary>
    /// Prevent spectators from broadcasting that they're opening the main entrance doors
    /// </summary>
    [HarmonyPatch(typeof(EntranceTeleport), nameof(EntranceTeleport.StartOpeningEntrance))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> SpectatorDisableDoorOpen(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchForward(false,
                new CodeMatch(OpCodes.Call,
                    Method(typeof(EntranceTeleport), nameof(EntranceTeleport.SyncStartOpeningDoorRpc))))
            .Set(OpCodes.Call, ((Action<EntranceTeleport>)SyncDoorIfNotDead).Method)
            .InstructionEnumeration();

        static void SyncDoorIfNotDead(EntranceTeleport entrance)
        {
            if (StartOfRound.Instance.localPlayerController.isPlayerDead)
                return;

            entrance.SyncStartOpeningDoorRpc();
        }
    }
}