using CullFactory.Data;
using HarmonyLib;
using LCVR.Patches;
using LCVR.Player;
using UnityEngine;

namespace LCVR.Compatibility.CullFactory;

[LCVRPatch(dependency: "CullFactory")]
[HarmonyPatch]
internal static class Patches
{
    /// <summary>
    /// Fix for CullFactory to include the VR helmet lights in the <see cref="DynamicObjects.allPlayerLights"/> array
    /// </summary>
    [HarmonyPatch(typeof(DynamicObjects), nameof(DynamicObjects.CollectAllPlayerLights))]
    [HarmonyPostfix]
    private static void OnCollectAllPlayerLights()
    {
        if (!VRSession.Instance)
            return;
        
        var clientId = VRSession.Instance.LocalPlayer.PlayerController.playerClientId;
        var lights = DynamicObjects.allPlayerLights[clientId];
        var cameraLights = VRSession.Instance.MainCamera.GetComponentsInChildren<Light>();
        
        DynamicObjects.allPlayerLights[clientId] = [..lights, ..cameraLights];
    }
}
