using HarmonyLib;
using LCVR.Input;
using LCVR.Player;
using System;
using UnityEngine.InputSystem;

namespace LCVR.Patches;

[LCVRPatch]
[HarmonyPatch]
internal class TerminalPatches
{
    private static Action<InputAction.CallbackContext> openMenuDelegate;

    /// <summary>
    /// Detect when the terminal is being used
    /// </summary>
    [HarmonyPatch(typeof(Terminal), nameof(Terminal.BeginUsingTerminal))]
    [HarmonyPostfix]
    private static void OnEnterTerminal()
    {
        VRSession.Instance.OnEnterTerminal();
    }

    /// <summary>
    /// Detect when the terminal is no longer being used
    /// </summary>
    [HarmonyPatch(typeof(Terminal), nameof(Terminal.QuitTerminal))]
    [HarmonyPostfix]
    private static void OnExitTerminal()
    {
        VRSession.Instance.OnExitTerminal();
    }

    /// <summary>
    /// Make sure the pause button exits the terminal
    /// </summary>
    [HarmonyPatch(typeof(Terminal), nameof(Terminal.OnEnable))]
    [HarmonyPostfix]
    private static void OnEnable(Terminal __instance)
    {
        openMenuDelegate = (Action<InputAction.CallbackContext>)Delegate.CreateDelegate(typeof(Action<InputAction.CallbackContext>), __instance, AccessTools.Method(typeof(Terminal), "PressESC"));

        Actions.Instance["OpenMenu"].performed += openMenuDelegate;
    }

    /// <summary>
    /// Make sure action event handlers are removed when the terminal script gets disabled
    /// </summary>
    [HarmonyPatch(typeof(Terminal), nameof(Terminal.OnDisable))]
    [HarmonyPostfix]
    private static void OnDisable(Terminal __instance)
    {
        if (openMenuDelegate == null || (Terminal)openMenuDelegate.Target != __instance)
            return;

        Actions.Instance["OpenMenu"].performed -= openMenuDelegate;
    }
}
