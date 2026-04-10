using HarmonyLib;
using LCVR.Managers;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace LCVR.Patches.Rendering;

[LCVRPatch]
[HarmonyPatch]
internal static class RenderingPatches
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

    /// <summary>
    /// Also render batched children in the custom camera
    /// </summary>
    [HarmonyPatch(typeof(BatchAllMeshChildren), nameof(BatchAllMeshChildren.RenderBatches))]
    [HarmonyPostfix]
    private static void RenderInCustomCamera(BatchAllMeshChildren __instance, bool renderOnHeadmountedCam)
    {
        if (renderOnHeadmountedCam || VRSession.Instance.CustomCamera is not {} camera)
            return;

        if (!StartOfRound.Instance.activeCamera.enabled)
            return;

        foreach (var batch in __instance.Batches)
            for (var i = 0; i < __instance.mesh.subMeshCount; i++)
                Graphics.DrawMeshInstanced(__instance.mesh, i, __instance.material, batch, null, ShadowCastingMode.On,
                    true, 24, camera);
    }
}
