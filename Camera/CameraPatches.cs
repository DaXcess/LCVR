using GameNetcodeStuff;
using HarmonyLib;
using LethalCompanyVR.Input;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace LethalCompanyVR
{
    [HarmonyPatch]
    public class CameraPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SwitchCamera))]
        private static void OnCameraSwitched()
        {
            Logger.LogDebug("SwitchCamera called");

            var mainCamera = StartOfRound.Instance.activeCamera;

            if (mainCamera == null)
            {
                Logger.LogError("Could not find Main Camera!");
                return;
            }

            var uiCamera = GameObject.Find("UICamera")?.GetComponent<Camera>();

            if (uiCamera == null)
            {
                Logger.LogError("Could not find UI Camera!");
                return;
            }

            GameObject.Find("PlayerHUDHelmetModel").transform.localScale = Vector3.zero;

            mainCamera.targetTexture = null;
            uiCamera.gameObject.SetActive(false);

            // TODO: Make configurable
            var hdCamera = mainCamera.GetComponent<HDAdditionalCameraData>();
            hdCamera.allowDeepLearningSuperSampling = false;
            hdCamera.allowDynamicResolution = true;

            // TODO: Cleanup
            // Manually walk to the player object because in multiplayer you are not "Player" but instead one of the other player objects
            mainCamera.gameObject.transform.parent.parent.parent.parent.gameObject.AddComponent<VRPlayer>();

            // TODO: HUD to world space
            var hudObject = new GameObject("VR HUD");
            var hud = hudObject.AddComponent<VRHUD>();

            hud.Initialize(mainCamera);

            // Input controls
            VRControls.InsertVRControls();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.SpawnPlayerAnimation))]
        private static bool OnPlayerSpawnAnimation()
        {
            Logger.Log("PlayerControllerB.SpawnPlayerAnimation called");

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Camera), "targetTexture")]
        [HarmonyPatch(MethodType.Setter)]
        private static bool UpdateCameraTargetTexture(Camera __instance, ref RenderTexture value)
        {
            if (StartOfRound.Instance.activeCamera == __instance)
                value = null;

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerControllerB), "SetHoverTipAndCurrentInteractTrigger")]
        private static bool SetHoverTipAndCurrentInteractTriggerPrefix()
        {
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerControllerB), "ClickHoldInteraction")]
        private static bool ClickHoldInteractionPrefix()
        {
            return false;
        }
    }
}
