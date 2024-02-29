using HarmonyLib;
using UnityEngine;
using System.Collections;
using LCVR.Networking;
using LCVR.Patches;
using LCVR.Player;

namespace LCVR;

[LCVRPatch]
[HarmonyPatch]
internal class VREntryPoint
{
    /// <summary>
    /// The entrypoint for when you join a game
    /// </summary>
    [HarmonyPatch(typeof(StartOfRound), "Start")]
    [HarmonyPostfix]
    private static void OnGameEntered()
    {
        StartOfRound.Instance.StartCoroutine(Start());
    }

    private static IEnumerator Start()
    {
        Logger.Log("Hello from VR!");

        yield return new WaitUntil(() => StartOfRound.Instance.activeCamera != null);
    }
}

[LCVRPatch(LCVRPatchTarget.Universal)]
[HarmonyPatch]
internal class UniversalEntryPoint
{
    [HarmonyPatch(typeof(StartOfRound), "Start")]
    [HarmonyPostfix]
    private static void OnGameEntered()
    {
        StartOfRound.Instance.StartCoroutine(Start());
    }

    private static IEnumerator Start()
    {
        Logger.Log("Hello from universal!");

        yield return new WaitUntil(() => StartOfRound.Instance.activeCamera != null);

        // Setup session manager (required for both VR and NonVR)
        new GameObject("LCVR Session Manager").AddComponent<VRSession>();

        // Setup Dissonance for VR movement comms
        yield return DNet.Initialize();
    }

    [HarmonyPatch(typeof(StartOfRound), "OnDestroy")]
    [HarmonyPostfix]
    private static void OnGameLeave()
    {
        DNet.Shutdown();
    }
}
