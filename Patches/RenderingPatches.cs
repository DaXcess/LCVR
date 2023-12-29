using HarmonyLib;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering.HighDefinition;

namespace LCVR.Patches
{
    [LCVRPatch]
    [HarmonyPatch]
    internal static class RenderingPatches
    {
        // HDRP Volumetric fog rendering is being rendered incorrectly. This is *NOT* a fix, it just reduces *SOME* of
        //  the obviously brighter rendering on the right eye. There are still issues. I have no clue how to fix them.

        private static bool isRenderingLeftEye = true;

        [HarmonyPatch(typeof(HDRenderPipeline), "VolumetricLightingPass")]
        [HarmonyPrefix]
        private static bool BeforeVolumetricLightingPass(ref TextureHandle __result, RenderGraph renderGraph, HDCamera hdCamera)
        {
            if (hdCamera.camera.name == "MainCamera")
            {
                if (!isRenderingLeftEye)
                {
                    __result = renderGraph.ImportTexture(HDUtils.clearTexture3DRTH);

                    isRenderingLeftEye = true;
                    return false;
                }

                isRenderingLeftEye = false;
            }

            return true;
        }
    }
}
