using HarmonyLib;
using LCVR.Assets;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace LCVR.Patches;

[LCVRPatch]
[HarmonyPatch]
internal static class UIPatches
{
    /// <summary>
    /// Show our custom VR init screen instead of the vanilla one
    /// </summary>
    [HarmonyPatch(typeof(PreInitSceneScript), nameof(PreInitSceneScript.Start))]
    [HarmonyPostfix]
    private static void OnPreInitMenuShown(PreInitSceneScript __instance)
    {
        SceneManager.LoadScene("LCVR Init Scene");
    }
    
    /// <summary>
    /// This function runs when the main menu is shown
    /// </summary>
    [HarmonyPatch(typeof(MenuManager), nameof(MenuManager.Start))]
    [HarmonyPostfix]
    private static void OnMainMenuShown(MenuManager __instance)
    {
        Object.Instantiate(__instance.isInitScene
            ? AssetManager.InitMenuEnvironment
            : AssetManager.MainMenuEnvironment);
    }

    /// <summary>
    /// Make sure the new input system is being used
    /// </summary>
    [HarmonyPatch(typeof(XRUIInputModule), nameof(XRUIInputModule.ProcessNavigation))]
    [HarmonyPrefix]
    private static void ForceNewInputSystem(XRUIInputModule __instance)
    {
        if (__instance.activeInputMode != XRUIInputModule.ActiveInputMode.InputSystemActions)
            __instance.activeInputMode = XRUIInputModule.ActiveInputMode.InputSystemActions;
    }
}

[LCVRPatch(LCVRPatchTarget.Universal)]
[HarmonyPatch]
internal static class UniversalUIPatches
{
    /// <summary>
    /// This function runs when the main menu is shown
    /// </summary>
    [HarmonyPatch(typeof(MenuManager), nameof(MenuManager.Start))]
    [HarmonyPostfix]
    private static void OnMainMenuShown(MenuManager __instance)
    {
        // TODO: Add VR settings into non-vr main menu somehow
    }
}
