using HarmonyLib;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
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
        /// This function runs when the main menu is shown
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MenuManager), "Start")]
        private static void OnMainMenuShown(MenuManager __instance)
        {
            try
            {
                var canvas = __instance.GetComponentInParent<Canvas>();
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

                input.actionsAsset = InputActionAsset.FromJson(Encoding.UTF8.GetString(Properties.Resources.inputs_vr_menu));

#if RELEASE
                // Having the game be focussed improves performance significantly
                MoveToForeground();
#endif
            }
            catch (Exception exception)
            {
                Logger.LogWarning($"Failed to move canvas to world space ({__instance.name}): {exception}");
            }
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
