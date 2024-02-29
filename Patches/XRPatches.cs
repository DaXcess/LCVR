using HarmonyLib;
using UnityEngine.InputSystem.XR;

namespace LCVR.Patches;

/// <summary>
/// Funny Non-NVIDIA BepInEx Entrypoint quick fix
/// </summary>
[LCVRPatch]
[HarmonyPatch]
internal static class XRPatches
{
    [HarmonyPatch(typeof(XRSupport), nameof(XRSupport.Initialize))]
    [HarmonyPrefix]
    private static bool OnBeforeInitialize()
    {
        return false;
    }
}
