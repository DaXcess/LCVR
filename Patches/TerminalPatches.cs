using HarmonyLib;
using LCVR.Input;
using LCVR.Player;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LCVR.Patches
{
    [LCVRPatch]
    [HarmonyPatch]
    internal class TerminalPatches
    {
        private static Action<InputAction.CallbackContext> openMenuDelegate;

        [HarmonyPatch(typeof(Terminal), "BeginUsingTerminal")]
        [HarmonyPostfix]
        private static void OnEnterTerminal()
        {
            var player = GameObject.FindObjectOfType<VRPlayer>();

            player.OnEnterTerminal();
        }

        [HarmonyPatch(typeof(Terminal), "QuitTerminal")]
        [HarmonyPostfix]
        private static void OnExitTerminal()
        {
            var player = GameObject.FindObjectOfType<VRPlayer>();

            player.OnExitTerminal();
        }

        [HarmonyPatch(typeof(Terminal), "OnEnable")]
        [HarmonyPostfix]
        private static void OnEnable(Terminal __instance)
        {
            openMenuDelegate = (Action<InputAction.CallbackContext>)Delegate.CreateDelegate(typeof(Action<InputAction.CallbackContext>), __instance, AccessTools.Method(typeof(Terminal), "PressESC"));

            Actions.FindAction("Movement/OpenMenu").performed += openMenuDelegate;
        }

        [HarmonyPatch(typeof(Terminal), "OnDisable")]
        [HarmonyPostfix]
        private static void OnDisable(Terminal __instance)
        {
            if (openMenuDelegate == null || (Terminal)openMenuDelegate.Target != __instance)
                return;

            Actions.FindAction("Movement/OpenMenu").performed -= openMenuDelegate;
        }
    }
}
