using GameNetcodeStuff;
using HarmonyLib;
using LethalCompanyVR.Player;
using UnityEngine;

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

            // TODO: Oh god why did they make it like this
            var hud = GameObject.Find("IngamePlayerHUD");

            if (hud == null)
            {
                Logger.LogError("Failed to find HUD, game will look weird");
                return;
            }

            hud.transform.localScale *= 0.25f;

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
