using System.Linq;
using HarmonyLib;
using LCVR.Player;
using UnityEngine;

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
        var doorPosition = Object.FindObjectsOfType<EntranceTeleport>().First(e =>
                e.entranceId == __instance.entranceId && e.isEntranceToBuilding != __instance.isEntranceToBuilding)
            .transform.position;
        var direction = (__instance.exitPoint.position - doorPosition).normalized;
        var angle = Vector3.SignedAngle(Vector3.forward, direction, Vector3.up) / 90;
        var roundedAngle = (Mathf.Sign(angle) > 0 ? Mathf.Ceil(angle) : Mathf.Floor(angle)) * 90;
        var rotation = roundedAngle - VRSession.Instance.MainCamera.transform.localEulerAngles.y;

        VRSession.Instance.LocalPlayer.TurningProvider.SetOffset(rotation);
    }
}