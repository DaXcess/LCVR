using System.Diagnostics;
using LCVR.Assets;
using LCVR.Patches;
using LCVR.Player;
using Microsoft.MixedReality.Toolkit.Experimental.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.XR;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace LCVR.UI;

public class ZMainMenu : MonoBehaviour
{
    private static readonly InputAction ToggleKeyBind = new(binding: "<Keyboard>/F8");
    
    private PreInitSceneScript preinitManager;
    private MenuManager manager;

    private NonNativeKeyboard keyboard;
    private Canvas primaryCanvas;
    private Camera mainCamera;

    private XRRayInteractor leftController;
    private XRRayInteractor rightController;
    
    private InfoScreen infoScreen;
    private ErrorScreen errorScreen;
    
#if DEBUG
    private DebugScreen debugScreen;
#endif
    
    private void Awake()
    {
        ToggleKeyBind.Enable();
        
        preinitManager = FindObjectOfType<PreInitSceneScript>();
        manager = FindObjectOfType<MenuManager>();

        if (preinitManager is not null)
        {
            InitializePreInit();
            return;
        }

        InitializeMainMenu();
        
        // Setup screens
        
        if (manager.isInitScene)
            return;
        
        infoScreen = manager.gameObject.AddComponent<InfoScreen>();
        errorScreen = manager.gameObject.AddComponent<ErrorScreen>();
        
        if (!Plugin.Config.IntroScreenSeen.Value && VRSession.InVR)
            infoScreen.Show();
        else if (!VRSession.InVR && Plugin.Flags.HasFlag(Flags.StartupFailed))
            errorScreen.Show();

#if DEBUG
        debugScreen = manager.gameObject.AddComponent<DebugScreen>();

        if (!manager.isInitScene && !infoScreen.Active && !errorScreen.Active)
            debugScreen.Show();
#endif
    }

    private void Update()
    {
        if (!manager || manager.isInitScene)
            return;
        
        if (ToggleKeyBind.WasPressedThisFrame())
            ToggleVR();
    }

    /// <summary>
    /// Perform initialization in the pre-init scenes
    /// </summary>
    private void InitializePreInit()
    {
        var canvas = preinitManager.launchSettingsPanelsContainer.GetComponentInParent<Canvas>();
        
        if (VRSession.InVR)
            InitializeWithCanvas(canvas);
        
        if (!Plugin.Flags.HasFlag(Flags.InvalidGameAssembly))
            return;
        
        var textObject =
            Instantiate(canvas.gameObject.Find("GameObject/LANOrOnline/OnlineButton/Text (TMP) (1)"));
        var text = textObject.GetComponent<TextMeshProUGUI>();

        text.transform.parent = preinitManager.launchSettingsPanelsContainer.transform;
        text.transform.localPosition = new Vector3(200, -30, 0);
        text.transform.localScale = Vector3.one;
        text.text = "[LCVR] Invalid Game Assembly Detected!\nYou are using an unsupported version of the game!";
        text.autoSizeTextContainer = true;
        text.color = new Color(0.9434f, 0.9434f, 0.0434f, 1);
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 18;
        text.raycastTarget = false;
    }

    /// <summary>
    /// Perform initialization in the main menu scenes
    /// </summary>
    private void InitializeMainMenu()
    {
        if (!VRSession.InVR)
            return;

        var canvas = manager.menuButtons.GetComponentInParent<Canvas>();

        InitializeWithCanvas(canvas);

        if (Compat.IsLoaded(Compat.MoreCompany))
            Compatibility.MoreCompany.MoreCompanyCompatibility.SetupMoreCompanyUIMainMenu();

        if (!manager.isInitScene)
            InitializeKeyboard(canvas);
    }
    
    // ReSharper disable Unity.PerformanceAnalysis
    private void ToggleVR()
    {
        infoScreen.Hide();
        errorScreen.Hide();

#if DEBUG
        debugScreen.Hide();
#endif
        
        var inVR = VRSession.InVR;

        // Cancel all rebind operations
        // IngamePlayerSettings.Instance.CancelRebind();
        // if (inVR && KeyRemapManager.Instance != null)
        //     KeyRemapManager.Instance.CancelRebind();
        
        IngamePlayerSettings.Instance.DiscardChangedSettings();
        
        // Disable many of the panels
        manager.DisableUIPanel(manager.PleaseConfirmChangesSettingsPanel);
        manager.DisableUIPanel(manager.KeybindsPanel);
        manager.DisableUIPanel(manager.transform.parent.Find("MenuContainer/SettingsPanel").gameObject);
        manager.DisableUIPanel(manager.transform.parent.Find("MenuContainer/CreditsPanel").gameObject);
        manager.DisableUIPanel(manager.transform.parent.Find("MenuContainer/DeleteFileConfirmation").gameObject);
        manager.DisableUIPanel(manager.transform.parent.Find("MenuContainer/LobbyHostSettings").gameObject);
        
        if (manager.transform.parent.Find("Panel(Clone)") is {} panel)
            manager.DisableUIPanel(panel.gameObject);
        
        manager.EnableUIPanel(manager.transform.parent.Find("MenuContainer/MainButtons").gameObject);
        
        Plugin.ToggleVR();

        if (inVR && !VRSession.InVR)
            LeftVR();
        else if (!inVR && VRSession.InVR)
            EnteredVR();

        if (!inVR && !VRSession.InVR && Plugin.Flags.HasFlag(Flags.StartupFailed))
            errorScreen.Show();

#if DEBUG
        if (!infoScreen.Active && !errorScreen.Active)
            debugScreen.Show();
#endif
    }

    private void LeftVR()
    {
        DeinitializeMenu();

        // if (KeyRemapManager.Instance != null)
        //     Destroy(KeyRemapManager.Instance);
        
        // Restore vanilla bindings
        InputPatches.RestoreOriginalBindings();

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void EnteredVR()
    {
        // Force load bindings
        InputPatches.OnCreateSettings(IngamePlayerSettings.Instance);
        IngamePlayerSettings.Instance.playerInput.actions.LoadBindingOverridesFromJson(Plugin.Config
            .ControllerBindingsOverride.Value);

        InitializeMainMenu();
        
        if (!Plugin.Config.IntroScreenSeen.Value && VRSession.InVR)
            infoScreen.Show();
        
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.None;
    }

    /// <summary>
    /// Sets up the canvas and camera for use in VR
    /// </summary>
    private void InitializeWithCanvas(Canvas canvas)
    {
        // Disable base input system in favor of the VR input system
        var input = GameObject.Find("EventSystem")?.GetComponent<InputSystemUIInputModule>();
        if (input != null)
            input.enabled = false;

        // Uh oh
        if (canvas == null)
        {
            Logger.LogError("VR menu initialization failed: Unable to locate canvas!");
            return;
        }

        mainCamera = GameObject.Find("UICamera")?.GetComponent<Camera>();
        if (mainCamera == null)
        {
            Logger.LogError("VR menu initialization failed: Unable to locate UI Camera!");
            return;
        }

        mainCamera.nearClipPlane = 0.0001f;
        mainCamera.gameObject.AttachHeadTrackedPoseDriver();
        mainCamera.transform.localScale = Vector3.one;

        Logger.LogDebug("Initialized main menu camera");

        // Position the main menu canvas in world 5 units away from the player

        canvas.transform.localScale = Vector3.one * 0.0085f;
        canvas.transform.position = new Vector3(0, 1, 5);
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = mainCamera;

        // var canvasFollow = canvas.gameObject.AddComponent<CanvasTransformFollow>();
        // canvasFollow.sourceTransform = mainCamera.transform;
        // canvasFollow.heightOffset = 1;

        // Allow canvas interactions using XR raycaster

        Destroy(canvas.GetComponent<GraphicRaycaster>());
        canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();

        leftController = new GameObject("Left Controller").CreateInteractorController(Utils.Hand.Left);
        rightController = new GameObject("Right Controller").CreateInteractorController(Utils.Hand.Right);

        leftController.rayOriginTransform.localRotation = Quaternion.Euler(60, 347, 90);
        rightController.rayOriginTransform.localRotation = Quaternion.Euler(60, 347, 270);

        primaryCanvas = canvas;

        XRSettings.eyeTextureResolutionScale = 1.2f;
    }
    
    /// <summary>
    /// Add a keyboard to the main menu
    /// </summary>
    private void InitializeKeyboard(Canvas canvas)
    {
        keyboard = Instantiate(AssetManager.Keyboard).GetComponent<NonNativeKeyboard>();

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

    /// <summary>
    /// Reverts the canvas and camera back to normal
    /// </summary>
    private void DeinitializeMenu()
    {
        var input = GameObject.Find("EventSystem")?.GetComponent<InputSystemUIInputModule>();
        if (input != null)
            input.enabled = true;
        
        // Revert camera
        Destroy(mainCamera.GetComponent<TrackedPoseDriver>());
        
        // Revert canvas
        Destroy(primaryCanvas.GetComponent<TrackedDeviceGraphicRaycaster>());
        // DestroyImmediate(primaryCanvas.GetComponent<CanvasTransformFollow>());
        primaryCanvas.gameObject.AddComponent<GraphicRaycaster>();
        primaryCanvas.transform.localScale = Vector3.one * 0.1822f;
        primaryCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        
        // Destroy controllers
        Destroy(leftController.gameObject);
        Destroy(rightController.gameObject);

        // MoreCompany revert
        if (Compat.IsLoaded(Compat.MoreCompany))
            Compatibility.MoreCompany.MoreCompanyCompatibility.RevertMoreCompanyUIMainMenu();
    }
    
    private class InfoScreen : MonoBehaviour
    {
        private GameObject screen;

        public bool Active => screen.activeSelf;
        
        private void Awake()
        {
            var menuContainer = GameObject.Find("MenuContainer");

            screen = Instantiate(menuContainer.Find("NewsPanel"), menuContainer.transform);
            screen.name = "VRIntoPanel";
            screen.transform.localPosition = new Vector3(-4.8199f, -1.78f, 1.4412f);
            screen.transform.localEulerAngles = Vector3.zero;
            screen.transform.localScale = Vector3.one;
            screen.GetComponent<Image>().color = Color.clear;
            screen.Find("Image").SetActive(false);

            var title = screen.Find("Panel/NotificationText").GetComponent<TextMeshProUGUI>();
            var description = screen.Find("Panel/DemoText").GetComponent<TextMeshProUGUI>();

            title.text = "Welcome to LCVR!";
            description.text =
                "Welcome! Thank you for downloading LCVR!\nIf you run into any issues, you can always hop on in the LCVR Discord server. Make sure to check if the mods you are using are compatible with LCVR.\n\nThis mod has taken a lot of time to write and is available completely for free, but if you'd like to donate to help support further development, you can do so with the button below.\n\n- DaXcess";

            var githubButtonObject = new GameObject("GithubLink");
            var kofiButtonObject = new GameObject("KofiLink");
            var discordButtonObject = new GameObject("DiscordLink");

            githubButtonObject.transform.parent = screen.Find("Panel").transform;
            githubButtonObject.transform.localPosition = new Vector3(-60, -105, 0);
            githubButtonObject.transform.localEulerAngles = Vector3.zero;
            githubButtonObject.transform.localScale = Vector3.one * 0.3f;

            kofiButtonObject.transform.parent = screen.Find("Panel").transform;
            kofiButtonObject.transform.localPosition = new Vector3(-100, -105, 0);
            kofiButtonObject.transform.localEulerAngles = Vector3.zero;
            kofiButtonObject.transform.localScale = Vector3.one * 0.3f;

            discordButtonObject.transform.parent = screen.Find("Panel").transform;
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

            var continueButton = screen.Find("Panel/ResponseButton").GetComponent<Button>();
            continueButton.onClick.AddListener(() =>
            {

                Plugin.Config.IntroScreenSeen.Value = true;

#if DEBUG
                FindObjectOfType<ZMainMenu>().debugScreen.Show();
#endif
            });

            screen.SetActive(false);
        }

        public void Show()
        {
            screen.SetActive(true);
        }
        
        public void Hide()
        {
            screen.SetActive(false);
        }
    }

    private class ErrorScreen : MonoBehaviour
    {
        private GameObject screen;
        private ZMainMenu mainMenu;

        public bool Active => screen.activeSelf;

        private void Awake()
        {
            mainMenu = FindObjectOfType<ZMainMenu>();
            
            var menuContainer = GameObject.Find("MenuContainer");
            screen = Instantiate(menuContainer.Find("NewsPanel"), menuContainer.transform);
            screen.name = "VRErrorScreen";
            screen.transform.localPosition = new Vector3(-4.8199f, -1.78f, 1.4412f);
            screen.transform.localEulerAngles = Vector3.zero;
            screen.transform.localScale = Vector3.one;
            screen.GetComponent<Image>().color = Color.clear;
            screen.Find("Image").SetActive(false);

            var title = screen.Find("Panel/NotificationText").GetComponent<TextMeshProUGUI>();
            var description = screen.Find("Panel/DemoText").GetComponent<TextMeshProUGUI>();

            title.fontSize = 24;
            title.text = "LCVR FAILED TO START!";
            description.text =
                "Something went wrong and we weren't able to launch the game in VR. If you want to play without VR, it is recommended to disable VR by pressing the button below. If you need help troubleshooting, grab a copy of the logs, which you can open using the button below.";

            var picture = screen.Find("Panel/Picture").GetComponent<Image>();
            picture.transform.SetSiblingIndex(0);
            picture.transform.localScale = Vector3.one * 0.4f;
            picture.transform.localPosition = new Vector3(196, 59, 1);
            picture.sprite = AssetManager.WarningImage;

            screen.SetActive(false);

            var continueButtonObj = screen.Find("Panel/ResponseButton");
            var disableVRButtonObj = Instantiate(continueButtonObj, continueButtonObj.transform.parent);
            var openLogsButtonObj = Instantiate(continueButtonObj, continueButtonObj.transform.parent);
            
            var continueButton = continueButtonObj.GetComponent<Button>();
            var disableVRButton = disableVRButtonObj.GetComponent<Button>();
            var openLogsButton = openLogsButtonObj.GetComponent<Button>();
            
            disableVRButton.GetComponentInChildren<TextMeshProUGUI>().text = "[ Disable VR ]";
            openLogsButton.GetComponentInChildren<TextMeshProUGUI>().text = "[ Open Logs ]";

            continueButtonObj.transform.localPosition = new Vector3(183.9756f, -211.6089f, 9.5942f);
            openLogsButtonObj.transform.localPosition = new Vector3(-189.3309f, -211.6089f, 9.5942f);
            
            continueButton.onClick.AddListener(() =>
            {
#if DEBUG
                mainMenu.debugScreen.Show();
#endif
            });
            disableVRButton.onClick.AddListener(() =>
            {
                Plugin.Config.DisableVR.Value = true;
                
#if DEBUG
                mainMenu.debugScreen.Show();
#endif
            });
            openLogsButton.onClick.AddListener(() =>
            {
                Process.Start("notepad.exe", Application.consoleLogPath);
                
#if DEBUG
                mainMenu.debugScreen.Show();
#endif
            });
        }

        public void Show()
        {
            screen.SetActive(true);
        }
        
        public void Hide()
        {
            screen.SetActive(false);
        }
    }

#if DEBUG
    internal class DebugScreen : MonoBehaviour
    {
        private static bool screenSeen;

        private GameObject screen;

        private void Awake()
        {
            var menuContainer = GameObject.Find("MenuContainer");
            screen = Instantiate(menuContainer.Find("NewsPanel"), menuContainer.transform);
            screen.name = "ModDebugPanel";
            screen.transform.localPosition = new Vector3(-4.8199f, -1.78f, 1.4412f);
            screen.transform.localEulerAngles = Vector3.zero;
            screen.transform.localScale = Vector3.one;
            screen.GetComponent<Image>().color = Color.clear;
            screen.Find("Image").SetActive(false);

            var title = screen.Find("Panel/NotificationText").GetComponent<TextMeshProUGUI>();
            var description = screen.Find("Panel/DemoText").GetComponent<TextMeshProUGUI>();

            title.text = "LCVR DEBUG BUILD!";
            description.text =
                "You are using a development version of LCVR! This means that some features might not work as advertised, or gameplay being affected in unexpected ways. Do not use this version if you wish to keep your save files intact!";

            var picture = screen.Find("Panel/Picture").GetComponent<Image>();
            picture.transform.SetSiblingIndex(0);
            picture.transform.localScale = Vector3.one * 0.4f;
            picture.transform.localPosition = new Vector3(196, 59, 1);
            picture.sprite = AssetManager.WarningImage;

            screen.SetActive(false);

            var continueButton = screen.Find("Panel/ResponseButton").GetComponent<Button>();
            continueButton.onClick.AddListener(() => { screenSeen = true; });
        }

        public void Show()
        {
            if (!screenSeen)
                screen.SetActive(true);
        }
        
        public void Hide()
        {
            screen.SetActive(false);
        }
    }
#endif
}