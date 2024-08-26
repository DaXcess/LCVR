using HarmonyLib;
using UnityEngine;
using System.Collections;
using LCVR.Patches;
using LCVR.Player;

namespace LCVR;

[LCVRPatch(LCVRPatchTarget.Universal)]
[HarmonyPatch]
internal static class Entrypoint
{
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Start))]
    [HarmonyPrefix]
    private static void OnGameEntered()
    {
        StartOfRound.Instance.StartCoroutine(Start());
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private static IEnumerator Start()
    {
        yield return new WaitUntil(() => StartOfRound.Instance.activeCamera != null);

        // Setup session manager (required for both VR and NonVR)
        new GameObject("LCVR Session Manager").AddComponent<VRSession>();
    }
}
