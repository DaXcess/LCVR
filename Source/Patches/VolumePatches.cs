using HarmonyLib;
using UnityEngine.Rendering.HighDefinition;

namespace LCVR.Patches;

[LCVRPatch]
[HarmonyPatch]
internal static class VolumePatches
{
    /// <summary>
    /// Fixes left-right eye rendering issues when using local volumetric fog
    /// </summary>
    [HarmonyPatch(typeof(LocalVolumetricFog), nameof(LocalVolumetricFog.OnEnable))]
    [HarmonyPostfix]
    private static void FixedBlendMode(LocalVolumetricFog __instance)
    {
        __instance.parameters.blendingMode = LocalVolumetricFogBlendingMode.Overwrite;
    }
}
