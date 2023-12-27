using HarmonyLib;
using UnityEngine;

namespace LCVR.Patches
{
    [LCVRPatch]
    [HarmonyPatch]
    public class CameraPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Camera), "targetTexture")]
        [HarmonyPatch(MethodType.Setter)]
        private static bool UpdateCameraTargetTexture(Camera __instance, ref RenderTexture value)
        {
            if (StartOfRound.Instance.activeCamera == __instance)
                value = null;

            return true;
        }
    }
}
