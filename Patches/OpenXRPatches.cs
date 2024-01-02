using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.XR.OpenXR.Input;

namespace LCVR.Patches
{
    [LCVRPatch]
    [HarmonyPatch]
    internal static class OpenXRPatches
    {
        [HarmonyPatch(typeof(OpenXRInput), "RegisterDevices")]
        [HarmonyPrefix]
        private static void OnRegisterDevices(object actionMaps)
        {
            var list = (IList)actionMaps;

            foreach (var value in list)
            {
                Logger.LogDebug(JsonUtility.ToJson(value));
            }
        }
    }
}
