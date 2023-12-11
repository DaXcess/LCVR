using HarmonyLib;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace LethalCompanyVR
{
    [HarmonyPatch]
    public class UIPatches
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        /// <summary>
        /// This function runs when the pre-init menu is shown
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PreInitSceneScript), "Start")]
        private static void OnPreInitMenuShown()
        {
            InitMenuScene();
        }

        /// <summary>
        /// This function runs when the main menu is shown
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MenuManager), "Start")]
        private static void OnMainMenuShown()
        {
            InitMenuScene();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(KepRemapPanel), "OnEnable")]
        private static void OnEnableKeyRemapPanel(KepRemapPanel __instance)
        {
            foreach (var remappableKey in __instance.remappableKeys)
            {
                foreach (var binding in remappableKey.currentInput.action.bindings)
                {
                    Logger.LogDebug($"{remappableKey.ControlName}: {remappableKey.currentInput.name} [{binding.path}]");
                }
            }
        }

        private static void InitMenuScene()
        {
            var canvas = GameObject.Find("Canvas")?.GetComponent<Canvas>();
            var input = GameObject.Find("EventSystem")?.GetComponent<InputSystemUIInputModule>();

            if (input == null)
            {
                Logger.LogWarning("Failed to find InputSystemUIInputModule, main menu will not look good!");
                return;
            }

            if (canvas == null)
            {
                Logger.LogWarning("Failed to find Canvas, main menu will not look good!");
                return;
            }

            var uiCamera = GameObject.Find("UICamera")?.GetComponent<Camera>();

            if (uiCamera == null)
            {
                Logger.LogWarning("Failed to find UICamera, main menu will not look good!");
                return;
            }

            VRCamera.InitializeHMDCamera(uiCamera);

            Logger.LogDebug("Initialized main menu camera");

            // Position the main menu canvas in world 5 units away from the player

            canvas.transform.localScale = Vector3.one * 0.0085f;
            canvas.transform.position = new Vector3(0, 1, 5);
            canvas.renderMode = RenderMode.WorldSpace;

            // :)
            GameObject.Instantiate(AssetManager.cockroach);

            input.actionsAsset = InputActionAsset.FromJson(Properties.Resources.inputs_vr_menu);

#if RELEASE
                // Having the game be focussed improves performance significantly
                MoveToForeground();
#endif
        }

        private static void MoveToForeground()
        {
            var proc = Process.GetCurrentProcess();

            // Hack to allow SetForegroundWindow to function
            keybd_event(0xA4, 0x45, 0x1, 0);
            keybd_event(0xA4, 0x45, 0x3, 0);
            
            SetForegroundWindow(proc.MainWindowHandle);
        }
    }
}
