using HarmonyLib;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine;
using System.Collections;
using LCVR.Networking;
using LCVR.Patches;
using LCVR.Player;
using LCVR.Assets;
using Microsoft.MixedReality.Toolkit.Experimental.UI;
using UnityEngine.InputSystem.UI;
using LCVR.Input;
using UnityEngine.InputSystem;

namespace LCVR
{
    [LCVRPatch]
    [HarmonyPatch]
    internal class VREntryPoint
    {
        /// <summary>
        /// The entrypoint for when you join a game
        /// </summary>
        [HarmonyPatch(typeof(StartOfRound), "Start")]
        [HarmonyPostfix]
        private static void OnGameEntered()
        {
            StartOfRound.Instance.StartCoroutine(Start());
        }

        private static IEnumerator Start()
        {
            Logger.Log("Hello from VR!");

            yield return new WaitUntil(() => StartOfRound.Instance.activeCamera != null);

            var mainCamera = StartOfRound.Instance.activeCamera;
            var uiCamera = GameObject.Find("UICamera")?.GetComponent<Camera>();

            if (uiCamera == null)
            {
                Logger.LogError("Could not find UI Camera!");
                yield break;
            }

            // Disable base UI input system
            var input = GameObject.Find("EventSystem")?.GetComponent<InputSystemUIInputModule>();

            if (input != null)
                input.enabled = false;

            // Disable first person helmet
            GameObject.Find("PlayerHUDHelmetModel").SetActive(false);

            // Disable ui camera and promote main camera
            mainCamera.targetTexture = null;
            uiCamera.GetComponent<HDAdditionalCameraData>().xrRendering = false;
            uiCamera.stereoTargetEye = StereoTargetEyeMask.None;
            uiCamera.enabled = false;

            mainCamera.stereoTargetEye = StereoTargetEyeMask.Both;
            mainCamera.GetComponent<HDAdditionalCameraData>().xrRendering = true;

            mainCamera.depth = uiCamera.depth + 1;

            // Apply optimization configuration
            var hdCamera = mainCamera.GetComponent<HDAdditionalCameraData>();
            hdCamera.allowDynamicResolution = Plugin.Config.EnableUpscaling.Value;
            hdCamera.allowDeepLearningSuperSampling = Plugin.Config.EnableDLSS.Value;

            Utils.DisableQualitySetting(hdCamera, FrameSettingsField.DepthOfField);

            if (Plugin.Config.DisableVolumetrics.Value)
                Utils.DisableQualitySetting(hdCamera, FrameSettingsField.Volumetrics);

            // Create desktop camera
            if (Plugin.Config.EnableCustomCamera.Value)
            {
                var children = mainCamera.transform.GetChildren();

                children.Do(child => child.SetParent(null, true));

                var customCamera = Object.Instantiate(mainCamera);
                customCamera.name = "Custom Camera";
                customCamera.transform.SetParent(mainCamera.transform, false);
                customCamera.transform.localEulerAngles = Vector3.zero;
                customCamera.transform.localScale = Vector3.one;

                customCamera.fieldOfView = Plugin.Config.CustomCameraFOV.Value;
                customCamera.depth++;
                customCamera.stereoTargetEye = StereoTargetEyeMask.None;
                customCamera.targetDisplay = 0;

                var hdDesktopCamera = customCamera.GetComponent<HDAdditionalCameraData>();
                hdDesktopCamera.xrRendering = false;

                children.Do(child => child.SetParent(mainCamera.transform, true));
            }

            // Initialize the VR player script
            var player = StartOfRound.Instance.localPlayerController.gameObject.AddComponent<VRPlayer>();

            // Initialize HUD
            player.hud = new GameObject("VR HUD Manager").AddComponent<VRHUD>();

            // Temporary: Update item offsets for certain items
            // Will eventually be replaced by VR interactions (two hand holding 'n stuff)
            Player.Items.UpdateVRControlsItemsOffsets();

            // Add VR keyboard to the Terminal
            var terminal = Object.FindObjectOfType<Terminal>();

            var keyboardObject = Object.Instantiate(AssetManager.keyboard);
            keyboardObject.transform.SetParent(terminal.transform.parent.parent, false);
            keyboardObject.transform.localPosition = new Vector3(-0.584f, 0.333f, 0.791f);
            keyboardObject.transform.localEulerAngles = new Vector3(0, 90, 90);
            keyboardObject.transform.localScale = Vector3.one * 0.0009f;

            keyboardObject.GetComponent<Canvas>().worldCamera = uiCamera;

            var keyboard = keyboardObject.GetComponent<NonNativeKeyboard>();
            keyboard.InputField = terminal.screenText;

            keyboard.OnKeyboardValueKeyPressed += (_) =>
            {
                RoundManager.PlayRandomClip(terminal.terminalAudio, terminal.keyboardClips);
            };

            keyboard.OnKeyboardFunctionKeyPressed += (_) =>
            {
                RoundManager.PlayRandomClip(terminal.terminalAudio, terminal.keyboardClips);
            };

            keyboard.OnTextSubmitted += (_, _) =>
            {
                terminal.OnSubmit();
            };

            keyboard.OnMacroTriggered += (text) =>
            {
                terminal.screenText.text = terminal.screenText.text.Substring(0, terminal.screenText.text.Length - terminal.textAdded);
                terminal.screenText.text += text;
                terminal.textAdded = text.Length;
                terminal.OnSubmit();
            };

            keyboard.OnClosed += (_, _) =>
            {
                terminal.QuitTerminal();
            };

            Actions.ReloadInputBindings();

#if DEBUG
            Experiments.Experiments.RunExperiments();
#endif

            if (!Plugin.Config.FirstTimeTipSeen.Value)
                HUDManager.Instance.StartCoroutine(FirstTimeTips());
        }

        private static IEnumerator FirstTimeTips()
        {
            HUDManager.Instance.DisplayTip("Welcome to VR!", "Now you can experience the horrors of Lethal Company in an immersive VR experience.");
            yield return new WaitForSeconds(5);

            HUDManager.Instance.DisplayTip("Basic movement", "To move, use your left joystick. You can sprint by pressing or holding down the left joystick.");
            yield return new WaitForSeconds(5);

            HUDManager.Instance.DisplayTip("Resetting height", "If your height is incorrect, you can recalibrate by pressing the Y button.");
            yield return new WaitForSeconds(5);

            HUDManager.Instance.DisplayTip("Too scared?", "Press the X button to open up the pause menu.");
            yield return new WaitForSeconds(5);

            HUDManager.Instance.DisplayTip("Switching items", "You can use the right joystick up/down to swap your items. Going left/right with the joystick will turn your player unless disabled.");
            yield return new WaitForSeconds(5);

            HUDManager.Instance.DisplayTip("Have fun!", "Good luck and have fun on your journey to the hellscapes of Lethal Company!");
            Plugin.Config.FirstTimeTipSeen.Value = true;
        }
    }

    [LCVRPatch(LCVRPatchTarget.Universal)]
    [HarmonyPatch]
    internal class UniversalEntryPoint
    {
        [HarmonyPatch(typeof(StartOfRound), "Start")]
        [HarmonyPostfix]
        private static void OnGameEntered()
        {
            StartOfRound.Instance.StartCoroutine(Start());
        }

        private static IEnumerator Start()
        {
            Logger.Log("Hello from universal!");

            yield return new WaitUntil(() => StartOfRound.Instance.activeCamera != null);

            // Setup Dissonance for VR movement comms
            DNet.SetupDissonanceNetworking();
        }

        [HarmonyPatch(typeof(StartOfRound), "OnDestroy")]
        [HarmonyPostfix]
        private static void OnGameLeave()
        {
            DNet.DestroyDissonanceNetworking();
        }
    }
}
