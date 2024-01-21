using HarmonyLib;
using LCVR.Assets;
using LCVR.Input;
using LCVR.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace LCVR.Patches
{
    [LCVRPatch]
    [HarmonyPatch]
    internal static class UIPatches
    {
        /// <summary>
        /// This function runs when the pre-init menu is shown
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PreInitSceneScript), "Start")]
        private static void OnPreInitMenuShown()
        {
            InitMenuScene();

            var canvas = GameObject.Find("Canvas");

            if (Plugin.Flags.HasFlag(Flags.UnityExplorerDetected))
            {
                var textObject = Object.Instantiate(canvas.Find("GameObject/LANOrOnline/OnlineButton/Text (TMP) (1)"));
                var text = textObject.GetComponent<TextMeshProUGUI>();

                text.transform.parent = canvas.Find("GameObject").transform;
                text.transform.localPosition = new Vector3(200, -100, 0);
                text.transform.localScale = Vector3.one;
                text.text = "Unity Explorer Detected!\nUI controls are most likely nonfunctional!";
                text.autoSizeTextContainer = true;
                text.color = new Color(0.9434f, 0.9434f, 0.0434f, 1);
                text.alignment = TextAlignmentOptions.Center;
                text.fontSize = 18;
                text.raycastTarget = false;
            }

            if (Plugin.Flags.HasFlag(Flags.InvalidGameAssembly))
            {
                var textObject = Object.Instantiate(canvas.Find("GameObject/LANOrOnline/OnlineButton/Text (TMP) (1)"));
                var text = textObject.GetComponent<TextMeshProUGUI>();

                text.transform.parent = canvas.Find("GameObject").transform;
                text.transform.localPosition = new Vector3(200, -30, 0);
                text.transform.localScale = Vector3.one;
                text.text = "Invalid Game Assembly Detected!\nYou are using an unsupported version of the game!";
                text.autoSizeTextContainer = true;
                text.color = new Color(0.9434f, 0.9434f, 0.0434f, 1);
                text.alignment = TextAlignmentOptions.Center;
                text.fontSize = 18;
                text.raycastTarget = false;
            }
        }

        /// <summary>
        /// This function runs when the main menu is shown
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MenuManager), "Start")]
        private static void OnMainMenuShown(MenuManager __instance)
        {
            InitMenuScene();

            if (__instance.isInitScene)
                return;

            DisableKeybindsSetting();

            if (!Plugin.Config.IntroScreenSeen.Value)
                InjectIntroScreen();

            if (Plugin.Compatibility.IsLoaded("MoreCompany"))
                Compatibility.MoreCompany.SetupMoreCompanyUI();

            InitializeKeyboard();
        }

        private static void InitMenuScene()
        {
            var canvas = GameObject.Find("Canvas")?.GetComponent<Canvas>();
            var input = GameObject.Find("EventSystem")?.GetComponent<InputSystemUIInputModule>();

            if (input != null)
                input.enabled = false;

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

            uiCamera.nearClipPlane = 0.0001f;
            uiCamera.gameObject.AttachHeadTrackedPoseDriver();
            uiCamera.transform.localScale = Vector3.one;

            Logger.LogDebug("Initialized main menu camera");

            // Position the main menu canvas in world 5 units away from the player

            canvas.transform.localScale = Vector3.one * 0.0085f;
            canvas.transform.position = new Vector3(0, 1, 5);
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = uiCamera;

            var canvasFollow = canvas.gameObject.AddComponent<CanvasTransformFollow>();
            canvasFollow.sourceTransform = uiCamera.transform;
            canvasFollow.heightOffset = 1;

            // Allow canvas interactions using XR raycaster

            Object.Destroy(canvas.GetComponent<GraphicRaycaster>());
            canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();

            var rightController = new GameObject("Right Controller");
            var leftController = new GameObject("Left Controller");

            rightController.CreateInteractorController("RightHand");
            leftController.CreateInteractorController("LeftHand");

            var leftTransform = leftController.GetComponent<XRRayInteractor>().rayOriginTransform;
            var rightTransform = rightController.GetComponent<XRRayInteractor>().rayOriginTransform;

            leftTransform.localRotation = Quaternion.Euler(60, 347, 90);
            rightTransform.localRotation = Quaternion.Euler(60, 347, 270);
        }

        private static void DisableKeybindsSetting()
        {
            var menuContainer = GameObject.Find("MenuContainer");
            var keybindingsButton = menuContainer.Find("SettingsPanel/KeybindingsButton")?.GetComponent<Button>();
            var keybindingsText = keybindingsButton.GetComponentInChildren<TextMeshProUGUI>();

            keybindingsButton.enabled = false;
            keybindingsText.color = new Color(0.5f, 0.5f, 0.5f);
            keybindingsText.text = "> Change keybinds (Disabled in VR)";
        }

        /// <summary>
        /// Add a keyboard to the main menu
        /// </summary>
        private static void InitializeKeyboard()
        {

            var canvas = GameObject.Find("Canvas")?.GetComponent<Canvas>();
            var keyboard = Object.Instantiate(AssetManager.keyboard);

            keyboard.transform.SetParent(canvas.transform, false);
            keyboard.transform.localPosition = new Vector3(0, -470, -40);
            keyboard.transform.localEulerAngles = new Vector3(13, 0, 0);
            keyboard.transform.localScale = Vector3.one * 0.8f;

            keyboard.Find("keyboard_Alpha/Deny_Button").SetActive(false);
            keyboard.Find("keyboard_Alpha/Confirm_Button").SetActive(false);

            canvas.gameObject.AddComponent<MainMenuKeyboard>();
        }

        private static void InjectIntroScreen()
        {
            var menuContainer = GameObject.Find("MenuContainer");

            var vrIntroPanel = Object.Instantiate(menuContainer.Find("NewsPanel"));
            vrIntroPanel.name = "VRIntoPanel";
            vrIntroPanel.transform.parent = menuContainer.transform;
            vrIntroPanel.transform.localPosition = new Vector3(-4.8199f, -1.78f, 1.4412f);
            vrIntroPanel.transform.localEulerAngles = Vector3.zero;
            vrIntroPanel.transform.localScale = Vector3.one;

            var backdrop = vrIntroPanel.Find("Image");
            backdrop.transform.localScale = new Vector3(10, 10, 1);

            var title = vrIntroPanel.Find("Panel/NotificationText").GetComponent<TextMeshProUGUI>();
            var description = vrIntroPanel.Find("Panel/DemoText").GetComponent<TextMeshProUGUI>();

            title.text = "Welcome to LCVR!";
            description.text = "Welcome! Thank you for downloading LCVR!\nIf you run into any issues, you can always hop on in the LCVR Discord server. Make sure to check if the mods you are using are compatible with LCVR.\n\nThis mod has taken a lot of time to write and is available completely for free, but if you'd like to donate to help support further development, you can do so with the button below.\n\n- DaXcess";

            var githubButtonObject = new GameObject("GithubLink");
            var kofiButtonObject = new GameObject("KofiLink");
            var discordButtonObject = new GameObject("DiscordLink");

            githubButtonObject.transform.parent = vrIntroPanel.Find("Panel").transform;
            githubButtonObject.transform.localPosition = new Vector3(-60, -105, 0);
            githubButtonObject.transform.localEulerAngles = Vector3.zero;
            githubButtonObject.transform.localScale = Vector3.one * 0.3f;

            kofiButtonObject.transform.parent = vrIntroPanel.Find("Panel").transform;
            kofiButtonObject.transform.localPosition = new Vector3(-100, -105, 0);
            kofiButtonObject.transform.localEulerAngles = Vector3.zero;
            kofiButtonObject.transform.localScale = Vector3.one * 0.3f;

            discordButtonObject.transform.parent = vrIntroPanel.Find("Panel").transform;
            discordButtonObject.transform.localPosition = new Vector3(-140, -105, 0);
            discordButtonObject.transform.localEulerAngles = Vector3.zero;
            discordButtonObject.transform.localScale = Vector3.one * 0.3f;

            var githubImage = githubButtonObject.AddComponent<Image>();
            var kofiImage = kofiButtonObject.AddComponent<Image>();
            var discordImage = discordButtonObject.AddComponent<Image>();

            githubImage.sprite = AssetManager.githubImage;
            kofiImage.sprite = AssetManager.kofiImage;
            discordImage.sprite = AssetManager.discordImage;

            var githubButton = githubButtonObject.AddComponent<Button>();
            var kofiButton = kofiButtonObject.AddComponent<Button>();
            var discordButton = discordButtonObject.AddComponent<Button>();

            var githubButtonColors = githubButton.colors;
            var kofiButtonColors = kofiButton.colors;
            var discordButtonColors = discordButton.colors;

            githubButtonColors.highlightedColor =
                kofiButtonColors.highlightedColor =
                discordButtonColors.highlightedColor = new Color(0.7f, 0.7f, 0.7f);

            githubButtonColors.pressedColor =
                kofiButtonColors.pressedColor =
                discordButtonColors.pressedColor = new Color(0.6f, 0.6f, 0.6f);

            githubButton.colors = githubButtonColors;
            kofiButton.colors = kofiButtonColors;
            discordButton.colors = discordButtonColors;

            githubButton.onClick.AddListener(() => Application.OpenURL("https://github.com/DaXcess/LCVR"));
            kofiButton.onClick.AddListener(() => Application.OpenURL("https://ko-fi.com/daxcess"));
            discordButton.onClick.AddListener(() => Application.OpenURL("https://discord.gg/2DxNgpPZUF"));

            var continueButton = vrIntroPanel.Find("Panel/ResponseButton").GetComponent<Button>();
            continueButton.onClick.AddListener(() =>
            {

                Plugin.Config.IntroScreenSeen.Value = true;

#if DEBUG
                if (!UniversalUIPatches.debugScreenSeen)
                    menuContainer.Find("ModDebugPanel").SetActive(true);
#endif
            });

            vrIntroPanel.SetActive(true);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(XRUIInputModule), "ProcessNavigation")]
        private static void ForceNewInputSystem(XRUIInputModule __instance)
        {
            if (__instance.activeInputMode != XRUIInputModule.ActiveInputMode.InputSystemActions)
            {
                __instance.activeInputMode = XRUIInputModule.ActiveInputMode.InputSystemActions;
            }
        }
    }

    [LCVRPatch(LCVRPatchTarget.Universal)]
    [HarmonyPatch]
    internal static class UniversalUIPatches
    {
        /// <summary>
        /// This function runs when the pre-init menu is shown
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PreInitSceneScript), "Start")]
        private static void OnPreInitMenuShown()
        {
            if (!Plugin.Flags.HasFlag(Flags.RestartRequired))
                return;

            var canvas = GameObject.Find("Canvas");
            var textObject = Object.Instantiate(canvas.Find("GameObject/LANOrOnline/OnlineButton/Text (TMP) (1)"));
            var text = textObject.GetComponent<TextMeshProUGUI>();

            text.transform.parent = canvas.Find("GameObject").transform;
            text.transform.localPosition = new Vector3(200, -170, 0);
            text.transform.localScale = Vector3.one;
            text.text = "VR Setup Complete!\nYou must restart your game to go into VR!\nYou can continue if you want to play without VR.";
            text.autoSizeTextContainer = true;
            text.color = new Color(0.9434f, 0.0434f, 0.0434f, 1);
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 18;
            text.raycastTarget = false;
        }


#if DEBUG
        internal static bool debugScreenSeen = false;

        /// <summary>
        /// This function runs when the main menu is shown
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MenuManager), "Start")]
        private static void OnMainMenuShown(MenuManager __instance)
        {
            if (__instance.isInitScene)
                return;

            InjectDebugScreen();
        }

        private static void InjectDebugScreen()
        {
            if (debugScreenSeen)
                return;

            var menuContainer = GameObject.Find("MenuContainer");
            var modDebugPanel = Object.Instantiate(menuContainer.Find("NewsPanel"));
            modDebugPanel.name = "ModDebugPanel";
            modDebugPanel.transform.parent = menuContainer.transform;
            modDebugPanel.transform.localPosition = new Vector3(-4.8199f, -1.78f, 1.4412f);
            modDebugPanel.transform.localEulerAngles = Vector3.zero;
            modDebugPanel.transform.localScale = Vector3.one;

            var backdrop = modDebugPanel.Find("Image");
            backdrop.transform.localScale = new Vector3(10, 10, 1);

            var title = modDebugPanel.Find("Panel/NotificationText").GetComponent<TextMeshProUGUI>();
            var description = modDebugPanel.Find("Panel/DemoText").GetComponent<TextMeshProUGUI>();

            title.text = "LCVR DEBUG BUILD!";
            description.text = "You are using a development version of LCVR! Expect this version of the mod to be highly unstable!";

            var picture = modDebugPanel.Find("Panel/Picture").GetComponent<Image>();
            picture.transform.SetSiblingIndex(0);
            picture.transform.localScale = Vector3.one * 0.4f;
            picture.transform.localPosition = new Vector3(196, 59, 1);
            picture.sprite = AssetManager.warningImage;

            modDebugPanel.SetActive(!Plugin.Flags.HasFlag(Flags.VR) || Plugin.Config.IntroScreenSeen.Value);

            var continueButton = modDebugPanel.Find("Panel/ResponseButton").GetComponent<Button>();
            continueButton.onClick.AddListener(() =>
            {
                debugScreenSeen = true;
            });
        }
#endif
    }
}
