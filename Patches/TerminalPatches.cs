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

    [HarmonyPatch(typeof(Terminal), "BeginUsingTerminal")]
    [HarmonyPostfix]
    private static void OnEnterTerminal()
    {
        VRSession.Instance.OnEnterTerminal();
    }

    [HarmonyPatch(typeof(Terminal), "QuitTerminal")]
    [HarmonyPostfix]
    private static void OnExitTerminal()
    {
        VRSession.Instance.OnExitTerminal();
    }

    [HarmonyPatch(typeof(Terminal), "OnEnable")]
    [HarmonyPostfix]
    private static void OnEnable(Terminal __instance)
    {
        openMenuDelegate = (Action<InputAction.CallbackContext>)Delegate.CreateDelegate(typeof(Action<InputAction.CallbackContext>), __instance, AccessTools.Method(typeof(Terminal), "PressESC"));

        Actions.Instance["Movement/OpenMenu"].performed += openMenuDelegate;
        Actions.Instance.OnReload += OnReloadActions;
    }

    [HarmonyPatch(typeof(Terminal), "OnDisable")]
    [HarmonyPostfix]
    private static void OnDisable(Terminal __instance)
    {
        if (openMenuDelegate == null || (Terminal)openMenuDelegate.Target != __instance)
            return;

        Actions.Instance["Movement/OpenMenu"].performed -= openMenuDelegate;
        Actions.Instance.OnReload -= OnReloadActions;
    }

    private static void OnReloadActions(InputActionAsset oldActions, InputActionAsset newActions)
    {
        oldActions["Movement/OpenMenu"].performed -= openMenuDelegate;
        newActions["Movement/OpenMenu"].performed += openMenuDelegate;
    }
}
