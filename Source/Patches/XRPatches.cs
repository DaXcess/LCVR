using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.XR.Management;
using static HarmonyLib.AccessTools;

namespace LCVR.Patches;

[LCVRPatch(LCVRPatchTarget.Universal)]
[HarmonyPatch]
internal static class XRPatches
{
    /// <summary>
    /// Make the occlusion mesh color black
    /// </summary>
    [HarmonyPatch(typeof(XRSystem), nameof(XRSystem.Initialize))]
    [HarmonyPostfix]
    private static void OnXRSystemInitialize()
    {
        XRSystem.s_OcclusionMeshMaterial?.SetVector("_ClearColor", Vector4.zero);
    }

    /// <summary>
    /// Make sure the game doesn't override our OpenXR features
    /// </summary>
    [HarmonyPatch(typeof(XRGeneralSettings), nameof(XRGeneralSettings.AttemptStartXRSDKOnBeforeSplashScreen))]
    [HarmonyPrefix]
    private static void OnBeforeOpenXRInitialize()
    {
        OpenXR.Loader.UpdateFeatures();
    }

    /// <summary>
    /// Injects an additional offset to the mirror view shader
    /// </summary>
    [HarmonyPatch(typeof(XRMirrorView), nameof(XRMirrorView.RenderMirrorView))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> MirrorViewOffsetPatch(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchForward(false,
                new CodeMatch(OpCodes.Ldsfld,
                    Field(typeof(XRMirrorView), nameof(XRMirrorView.s_MirrorViewMaterialProperty))))
            .Advance(-27)
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloca_S, (byte)5),
                new CodeInstruction(OpCodes.Call, ((ApplyUVOffsetDelegate)ApplyUVOffset).Method)
            )
            .InstructionEnumeration();
    }

    private delegate void ApplyUVOffsetDelegate(ref Vector4 srcRect);
    private static void ApplyUVOffset(ref Vector4 srcRect)
    {
        srcRect += new Vector4(0, 0, 1, 0) * Plugin.Config.MirrorXOffset.Value +
                   new Vector4(0, 0, 0, 1) * Plugin.Config.MirrorYOffset.Value;
    }
}
