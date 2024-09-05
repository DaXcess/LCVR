using HarmonyLib;
using LCVR.Assets;
using LCVR.Player;
using LCVR.UI;
using LCVR.UI.Settings;
using Microsoft.MixedReality.Toolkit.Experimental.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace LCVR.Patches;

[LCVRPatch]
[HarmonyPatch]
internal static class UIPatches
{
    /// <summary>
    /// This function runs when the pre-init menu is shown
    /// </summary>
    [HarmonyPatch(typeof(PreInitSceneScript), nameof(PreInitSceneScript.Start))]
    [HarmonyPostfix]
    private static void OnPreInitMenuShown(PreInitSceneScript __instance)
    {
        var canvas = __instance.launchSettingsPanelsContainer.GetComponentInParent<Canvas>();

        InitMenuScene(canvas);

        if (!Plugin.Flags.HasFlag(Flags.InvalidGameAssembly))
            return;

        var textObject =
            Object.Instantiate(canvas.gameObject.Find("GameObject/LANOrOnline/OnlineButton/Text (TMP) (1)"));
        var text = textObject.GetComponent<TextMeshProUGUI>();

        text.transform.parent = __instance.launchSettingsPanelsContainer.transform;
        text.transform.localPosition = new Vector3(200, -30, 0);
        text.transform.localScale = Vector3.one;
        text.text = "Invalid Game Assembly Detected!\nYou are using an unsupported version of the game!";
        text.autoSizeTextContainer = true;
        text.color = new Color(0.9434f, 0.9434f, 0.0434f, 1);
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 18;
        text.raycastTarget = false;
    }

    /// <summary>
    /// This function runs when the main menu is shown
    /// </summary>
    [HarmonyPatch(typeof(MenuManager), nameof(MenuManager.Start))]
    [HarmonyPrefix]
    private static void OnMainMenuShown(MenuManager __instance)
    {
        var canvas = __instance.menuButtons.GetComponentInParent<Canvas>();

        InitMenuScene(canvas);

        if (Compat.IsLoaded(Compat.MoreCompany))
            Compatibility.MoreCompany.MoreCompanyCompatibility.SetupMoreCompanyUI();

        if (__instance.isInitScene)
            return;

        if (!Plugin.Config.IntroScreenSeen.Value)
            InjectIntroScreen();

        InitializeKeyboard(canvas);
    }

    private static void InitMenuScene(Canvas canvas)
    {
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

        var leftControllerInteractor = new GameObject("Left Controller").CreateInteractorController(Utils.Hand.Left);
        var rightControllerInteractor = new GameObject("Right Controller").CreateInteractorController(Utils.Hand.Right);

        leftControllerInteractor.rayOriginTransform.localRotation = Quaternion.Euler(60, 347, 90);
        rightControllerInteractor.rayOriginTransform.localRotation = Quaternion.Euler(60, 347, 270);

        XRSettings.eyeTextureResolutionScale = 1.2f;
    }

    /// <summary>
    /// Add a keyboard to the main menu
    /// </summary>
    private static void InitializeKeyboard(Canvas canvas)
    {
        var keyboard = Object.Instantiate(AssetManager.Keyboard).GetComponent<NonNativeKeyboard>();

        keyboard.transform.SetParent(canvas.transform, false);
        keyboard.transform.localPosition = new Vector3(0, -470, -40);
        keyboard.transform.localEulerAngles = new Vector3(13, 0, 0);
        keyboard.transform.localScale = Vector3.one * 0.8f;

        keyboard.gameObject.Find("keyboard_Alpha/Deny_Button").SetActive(false);
        keyboard.gameObject.Find("keyboard_Alpha/Confirm_Button").SetActive(false);

        keyboard.SubmitOnEnter = true;

        var component = canvas.gameObject.AddComponent<Keyboard>();
        component.keyboard = keyboard;
    }

    private static void InjectIntroScreen()
    {
        var menuContainer = GameObject.Find("MenuContainer");

        var vrIntroPanel = Object.Instantiate(menuContainer.Find("NewsPanel"), menuContainer.transform);
        vrIntroPanel.name = "VRIntoPanel";
        vrIntroPanel.transform.localPosition = new Vector3(-4.8199f, -1.78f, 1.4412f);
        vrIntroPanel.transform.localEulerAngles = Vector3.zero;
        vrIntroPanel.transform.localScale = Vector3.one;

        var backdrop = vrIntroPanel.Find("Image");
        backdrop.transform.localScale = new Vector3(10, 10, 1);

        var title = vrIntroPanel.Find("Panel/NotificationText").GetComponent<TextMeshProUGUI>();
        var description = vrIntroPanel.Find("Panel/DemoText").GetComponent<TextMeshProUGUI>();

        title.text = "Welcome to LCVR!";
        description.text =
            "Welcome! Thank you for downloading LCVR!\nIf you run into any issues, you can always hop on in the LCVR Discord server. Make sure to check if the mods you are using are compatible with LCVR.\n\nThis mod has taken a lot of time to write and is available completely for free, but if you'd like to donate to help support further development, you can do so with the button below.\n\n- DaXcess";

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

        githubImage.sprite = AssetManager.GithubImage;
        kofiImage.sprite = AssetManager.KofiImage;
        discordImage.sprite = AssetManager.DiscordImage;

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

    /// <summary>
    /// Make sure the new input system is being used
    /// </summary>
    [HarmonyPatch(typeof(XRUIInputModule), nameof(XRUIInputModule.ProcessNavigation))]
    [HarmonyPrefix]
    private static void ForceNewInputSystem(XRUIInputModule __instance)
    {
        if (__instance.activeInputMode != XRUIInputModule.ActiveInputMode.InputSystemActions)
            __instance.activeInputMode = XRUIInputModule.ActiveInputMode.InputSystemActions;
    }
}

[LCVRPatch(LCVRPatchTarget.Universal)]
[HarmonyPatch]
internal static class UniversalUIPatches
{
#if DEBUG
    internal static bool debugScreenSeen;
#endif

    /// <summary>
    /// This function runs when the main menu is shown
    /// </summary>
    [HarmonyPatch(typeof(MenuManager), nameof(MenuManager.Start))]
    [HarmonyPostfix]
    private static void OnMainMenuShown(MenuManager __instance)
    {
        if (__instance.isInitScene)
            return;

#if DEBUG
        InjectDebugScreen();
#endif
    }

    /// <summary>
    /// Create the VR settings menu when the UI loads
    /// </summary>
    [HarmonyPatch(typeof(SettingsOption), nameof(SettingsOption.OnEnable))]
    [HarmonyPostfix]
    private static void InjectSettingsMenu(SettingsOption __instance)
    {
        if (Plugin.Config.DisableSettingsButton.Value || __instance.name is not "SetToDefault")
            return;
        
        var isInGame = SceneManager.GetActiveScene().name is not "MainMenu";
        if (isInGame && !VRSession.InVR)
            return;
        
        Object.Destroy(__instance);

        var buttonObject = Object.Instantiate(__instance.gameObject, __instance.transform.parent);
        var button = buttonObject.GetComponent<Button>();

        buttonObject.name = "VRSettings";
        buttonObject.transform.localPosition += Vector3.up * 36;
        buttonObject.GetComponentInChildren<TextMeshProUGUI>().text = "> VR Settings";

        var settingsPanel = Object.Instantiate(AssetManager.SettingsPanel,
            isInGame ? __instance.transform.parent.parent : __instance.transform.parent.parent.parent);
        var settingsManager = settingsPanel.GetComponent<SettingsManager>();

        if (isInGame)
        {
            settingsManager.DisableCategory("interaction");
            settingsManager.DisableCategory("car");
        }
        
        settingsPanel.transform.localPosition = Vector3.zero;
        settingsPanel.transform.localEulerAngles = Vector3.zero;
        settingsPanel.transform.localScale = Vector3.one;
        settingsPanel.transform.SetSiblingIndex(6);
        settingsPanel.SetActive(false);

        button.onClick.RemoveAllListeners();
        button.onClick.m_PersistentCalls.Clear();
        button.onClick.AddListener(() =>
        {
            settingsManager.PlayButtonPressSFX();
            settingsPanel.SetActive(true);
        });
    }

    /// <summary>
    /// Make sure the VR settings menu is closed when the pause menu is closed
    /// </summary>
    [HarmonyPatch(typeof(QuickMenuManager), nameof(QuickMenuManager.CloseQuickMenu))]
    [HarmonyPostfix]
    private static void CloseVRSettingsOnUnpause()
    {
        Object.FindObjectOfType<SettingsManager>(true)?.gameObject.SetActive(false);
    }

#if DEBUG
    private static void InjectDebugScreen()
    {
        if (debugScreenSeen)
            return;

        var menuContainer = GameObject.Find("MenuContainer");
        var modDebugPanel = Object.Instantiate(menuContainer.Find("NewsPanel"), menuContainer.transform);
        modDebugPanel.name = "ModDebugPanel";
        modDebugPanel.transform.localPosition = new Vector3(-4.8199f, -1.78f, 1.4412f);
        modDebugPanel.transform.localEulerAngles = Vector3.zero;
        modDebugPanel.transform.localScale = Vector3.one;

        var backdrop = modDebugPanel.Find("Image");
        backdrop.transform.localScale = new Vector3(10, 10, 1);

        var title = modDebugPanel.Find("Panel/NotificationText").GetComponent<TextMeshProUGUI>();
        var description = modDebugPanel.Find("Panel/DemoText").GetComponent<TextMeshProUGUI>();

        title.text = "LCVR DEBUG BUILD!";
        description.text =
            "You are using a development version of LCVR! This means that some features might not work as advertised, or gameplay being affected in unexpected ways. Do not use this version if you wish to keep your save files intact!";

        var picture = modDebugPanel.Find("Panel/Picture").GetComponent<Image>();
        picture.transform.SetSiblingIndex(0);
        picture.transform.localScale = Vector3.one * 0.4f;
        picture.transform.localPosition = new Vector3(196, 59, 1);
        picture.sprite = AssetManager.WarningImage;

        modDebugPanel.SetActive(!VRSession.InVR || Plugin.Config.IntroScreenSeen.Value);

        var continueButton = modDebugPanel.Find("Panel/ResponseButton").GetComponent<Button>();
        continueButton.onClick.AddListener(() => { debugScreenSeen = true; });
    }
#endif
}
