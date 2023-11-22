using GameNetcodeStuff;
using HarmonyLib;
using LethalCompanyVR.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

namespace LethalCompanyVR
{
    [HarmonyPatch]
    public class CameraPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SwitchCamera))]
        private static void OnCameraSwitched()
        {
            if (!Plugin.VR_ENABLED) return;

            Logger.LogDebug("SwitchCamera called");

            Plugin.MainCamera = StartOfRound.Instance.activeCamera;

            if (Plugin.MainCamera == null)
            {
                Logger.LogError("Where is StartOfRound.activeCamera?!?!?");
                return;
            }

            // TODO: I'm guessing this ain't always the correct FOV
            if (Plugin.RenderCamera != null)
            {
                Plugin.MainCamera.fieldOfView = Plugin.RenderCamera.fieldOfView;
            }

            Plugin.MainCamera.stereoTargetEye = StereoTargetEyeMask.Both;

            
            // TODO: Keep helmet model once we know how to render it properly
            //var helmet = GameObject.Find("PlayerHUDHelmetModel");

            //if (helmet)
            //{
            //    helmet.SetActive(false);
            //}

            // TODO: Oh god why did they make it like this
            // var ui = GameObject.Find("UI");
            var hud = GameObject.Find("IngamePlayerHUD");

            if (hud == null /* || ui == null */)
            {
                Logger.LogError("Failed to find HUD, game will look weird");
                return;
            }

            // Maybe this was all that was necessary idk lol
            // TODO: Test this
            hud.transform.localScale *= 0.25f;

            // TODO: Disable chat maybe?

            // var canvasObject = new GameObject("HUDCanvas");
            // var canvas = canvasObject.AddComponent<Canvas>();

            // hud.transform.parent = canvasObject.transform;

            // var imageObject = new GameObject("RedSquare");
            // imageObject.transform.parent = hud.transform;

            // var image = imageObject.AddComponent<Image>();
            // image.color = Color.red;

            // var rect = image.GetComponent<RectTransform>();
            // rect.localPosition = new Vector3(0, 0, 0);
            // rect.sizeDelta = new Vector2(480, 480);

            // AttachedUI.Create(canvas, 0.00085f);

            VRPlayer.InitializeXRRig();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.SpawnPlayerAnimation))]
        private static bool OnPlayerSpawnAnimation()
        {
            if (!Plugin.VR_ENABLED) return true;

            Logger.Log("PlayerControllerB.SpawnPlayerAnimation called");

            return false;
        }
    }
}
