using HarmonyLib;
using LCVR.Managers;

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
}