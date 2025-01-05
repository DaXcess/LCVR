﻿using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LCVR.Patches;

[LCVRPatch]
[HarmonyPatch]
internal static class CameraPatches
{
    /// <summary>
    /// Prevents the game camera from setting a target texture, which would make the output not render to the headset
    /// </summary>
    [HarmonyPatch(typeof(Camera), nameof(Camera.targetTexture), MethodType.Setter)]
    [HarmonyPrefix]
    private static bool UpdateCameraTargetTexture(Camera __instance, ref RenderTexture value)
    {
        if (SceneManager.GetActiveScene().name is "ColdOpen1" or "ColdOpen2" ||
            StartOfRound.Instance.activeCamera == __instance)
            value = null;

        return true;
    }
}
