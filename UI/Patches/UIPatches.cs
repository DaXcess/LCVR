using HarmonyLib;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LethalCompanyVR
{
    [HarmonyPatch]
    public class UIPatches
    {
        public static Camera UICamera;

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
                var camera = GameObject.Find("UICamera").GetComponent<Camera>();
                var canvas = __instance.GetComponentInParent<Canvas>();

                if (canvas == null)
                {
                    Logger.LogWarning("Failed to find Canvas, main menu will not look good!");
                    return;
                }

                if (camera == null)
                {
                    Logger.LogWarning("Failed to find UICamera, main menu will not look good!");
                    return;
                }

                VRCamera.InitializeHMDCamera(camera);

                // Position the main menu canvas in world 5 units away from the player

                canvas.transform.localScale = Vector3.one * 0.0085f;
                canvas.transform.position = new Vector3(5, 0, 0);
                canvas.renderMode = RenderMode.WorldSpace;
            }
            catch (Exception exception)
            {
                Logger.LogWarning($"Failed to move canvas to world space ({__instance.name}): {exception}");
            }
        }
    }
}
