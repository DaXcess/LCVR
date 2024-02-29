using HarmonyLib;
using LCVR.Player;
using System.Collections;
using UnityEngine;
using UnityEngine.XR;

namespace LCVR.Patches;

[LCVRPatch]
[HarmonyPatch]
internal static class SoundManagerPatches
{
    private static Coroutine heartbeatCoroutine;

    /// <summary>
    /// Vibrate the controllers when the heartbeat audio is present
    /// </summary>
    [HarmonyPatch(typeof(SoundManager), nameof(SoundManager.SetFearAudio))]
    [HarmonyPostfix]
    private static void HapticHeartbeat(SoundManager __instance, bool ___playingHeartbeat)
    {
        if (___playingHeartbeat && __instance.heartbeatTimer + Time.deltaTime >= __instance.currentHeartbeatInterval)
        {
            if (heartbeatCoroutine != null)
                __instance.StopCoroutine(heartbeatCoroutine);

            heartbeatCoroutine = __instance.StartCoroutine(HeartbeatCoroutine());
        }
    }

    private static IEnumerator HeartbeatCoroutine()
    {
        VRSession.VibrateController(XRNode.LeftHand, 0.15f, 0.3f);
        VRSession.VibrateController(XRNode.RightHand, 0.15f, 0.3f);

        yield return new WaitForSeconds(0.3f);

        VRSession.VibrateController(XRNode.LeftHand, 0.15f, 0.5f);
        VRSession.VibrateController(XRNode.RightHand, 0.15f, 0.5f);

        heartbeatCoroutine = null;
    }
}
