using HarmonyLib;
using System;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace LethalCompanyVR
{
    [HarmonyPatch]
    public class UIPatches
    {
        // TODO: Remove after mod is done
        /// <summary>
        /// For ease of use, when VR is enabled immediately choose the online option in favor of LAN
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PreInitSceneScript), "Start")]
        private static void ImmediateOnline()
        {
            if (!Plugin.VR_ENABLED) return;

            SceneManager.LoadScene("InitScene");
        }

        // TODO: Clean up, especially the AttachedUI part in here
        /// <summary>
        /// This function runs when the main menu is shown
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MenuManager), "Start")]
        private static void OnMainMenuShown(MenuManager __instance)
        {
            if (!Plugin.VR_ENABLED) return;

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

                if (Plugin.RenderCamera == null)
                {
                    Logger.LogWarning("Failed to find UICamera, main menu will not look good!");
                    return;
                }

                VRCamera.InitializeHMDCamera(Plugin.RenderCamera);

                Logger.LogDebug("Initialized main menu camera");

                // Position the main menu canvas in world 5 units away from the player

                canvas.transform.localScale = Vector3.one * 0.0085f;
                canvas.transform.position = new Vector3(0, 1, 5);
                canvas.renderMode = RenderMode.WorldSpace;

                input.actionsAsset = InputActionAsset.FromJson(Encoding.UTF8.GetString(Properties.Resources.inputs_vr_menu));
            }
            catch (Exception exception)
            {
                Logger.LogWarning($"Failed to move canvas to world space ({__instance.name}): {exception}");
            }

            if (Plugin.FORCE_INGAME) __instance.ConfirmHostButton();
        }
    }
}
