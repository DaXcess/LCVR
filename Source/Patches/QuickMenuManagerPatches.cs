using HarmonyLib;
using LCVR.Managers;
using LCVR.UI.Controls;
using UnityEngine;

namespace LCVR.Patches;

[LCVRPatch]
[HarmonyPatch]
internal static class QuickMenuManagerPatches
{
    /// <summary>
    /// Detect when the pause menu opens
    /// </summary>
    [HarmonyPatch(typeof(QuickMenuManager), nameof(QuickMenuManager.OpenQuickMenu))]
    [HarmonyPostfix]
    private static void AfterOpenPauseMenu()
    {
        VRSession.Instance.OnPauseMenuOpened();
    }

    /// <summary>
    /// Detect when the pause menu closes
    /// </summary>
    [HarmonyPatch(typeof(QuickMenuManager), nameof(QuickMenuManager.CloseQuickMenu))]
    [HarmonyPostfix]
    private static void AfterClosePauseMenu(bool __runOriginal)
    {
        if (!__runOriginal)
            return;
        
        VRSession.Instance.OnPauseMenuClosed();
    }

    /// <summary>
    /// Prevent closing the pause menu under certain conditions
    /// </summary>
    [HarmonyPatch(typeof(QuickMenuManager), nameof(QuickMenuManager.CloseQuickMenu))]
    [HarmonyPrefix]
    private static bool BeforeClosePauseMenu()
    {
        // Disallow during rebinding operation
        if (Object.FindObjectOfType<ControlsManager>() is not { } manager)
            return true;
        
        return !manager.IsRebinding;
    }
}
