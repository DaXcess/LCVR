﻿using HarmonyLib;
using UnityEngine;
using System.Collections;
using LCVR.Networking;
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

    private static IEnumerator Start()
    {
        Logger.Log("Hello game, I am going to initialize now!");

        yield return new WaitUntil(() => StartOfRound.Instance.activeCamera != null);

        // Setup session manager (required for both VR and NonVR)
        new GameObject("LCVR Session Manager").AddComponent<VRSession>();

        // Setup Dissonance for VR movement comms
        yield return DNet.Initialize();
    }

    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnDestroy))]
    [HarmonyPostfix]
    private static void OnGameLeave()
    {
        DNet.Shutdown();
    }
}
