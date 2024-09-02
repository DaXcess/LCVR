using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;

namespace LCVR.Patches;

[LCVRPatch]
[HarmonyPatch]
internal static class CursorPatches
{
    /// <summary>
    /// Prevent the cursor from showing up when playing in VR 
    /// </summary>
    [HarmonyPatch(typeof(Cursor), nameof(Cursor.visible), MethodType.Setter)]
    [HarmonyPrefix]
    private static void PatchCursorVisibility(ref bool value)
    {
        if (Chainloader.PluginInfos.ContainsKey("com.sinai.unityexplorer"))
            return;
        
        value = false;
    }

    /// <summary>
    /// Prevent the cursor from getting locked when playing in VR
    /// </summary>
    [HarmonyPatch(typeof(Cursor), nameof(Cursor.lockState), MethodType.Setter)]
    [HarmonyPrefix]
    private static void PatchCursorLockState(ref CursorLockMode value)
    {
        if (Chainloader.PluginInfos.ContainsKey("com.sinai.unityexplorer"))
            return;
        
        value = CursorLockMode.None;
    }
}
