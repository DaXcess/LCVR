using Diversity.Misc;
using HarmonyLib;
using LCVR.Patches;

namespace LCVR.Compatibility.Diversity;

[LCVRPatch(dependency: Compat.Diversity)]
[HarmonyPatch]
internal static class DiversityPatches
{
    [HarmonyPatch(typeof(HUDManagerRevamp), nameof(HUDManagerRevamp.Start))]
    [HarmonyPostfix]
    private static void OnHUDManagerRevampStart()
    {
        DisableGlitchCustomPass();
    }
    
    [HarmonyPatch(typeof(HUDManagerRevamp), nameof(HUDManagerRevamp.Cpp_OnLoad))]
    [HarmonyPostfix]
    private static void OnLoadCustomPostProcess()
    {
        DisableGlitchCustomPass();
    }
    
    /// <summary>
    /// Disables the glitch fullscreen pass, which is incompatible with Dynamic Resolution
    /// </summary>
    private static void DisableGlitchCustomPass()
    {
        HUDManagerRevamp.Instance.fullscreenPass2.enabled = false;
    }
}