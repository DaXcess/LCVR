using LCVR.Assets;
using LCVR.Player;
using Microsoft.MixedReality.Toolkit.Experimental.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace LCVR.UI;

public class VRHUD : MonoBehaviour
{
    private Canvas worldInteractionCanvas;
    private Canvas leftHandCanvas;
    private Canvas rightHandCanvas;

    private Canvas spectateCanvas;

    private ObjectScanner scanner;
    public NonNativeKeyboard menuKeyboard;
    public NonNativeKeyboard terminalKeyboard;

    private GameObject selfRed;
    private GameObject self;
    private GameObject sprintMeter;
    private GameObject redGlowBodyParts;
    private GameObject weightUi;
    private GameObject clock;
    private GameObject battery;
    private GameObject inventory;
    private GameObject deathScreen;

    private GameObject spectatorLight;

    private void Awake()
    {
        // Create canvasses
        worldInteractionCanvas = new GameObject("World Interaction Canvas").AddComponent<Canvas>();
        worldInteractionCanvas.worldCamera = VRSession.Instance.MainCamera;
        worldInteractionCanvas.renderMode = RenderMode.WorldSpace;
        worldInteractionCanvas.transform.localScale = Vector3.one * 0.0066f;
        worldInteractionCanvas.gameObject.layer = LayerMask.NameToLayer("UI");

        var xOffset = Plugin.Config.HUDOffsetX.Value;
        var yOffset = Plugin.Config.HUDOffsetY.Value;

        if (!Plugin.Config.DisableArmHUD.Value)
        {
            leftHandCanvas = new GameObject("Left Hand Canvas").AddComponent<Canvas>();
            leftHandCanvas.worldCamera = VRSession.Instance.MainCamera;
            leftHandCanvas.renderMode = RenderMode.WorldSpace;
            leftHandCanvas.transform.localScale = Vector3.one * 0.001f;
            leftHandCanvas.gameObject.layer = LayerMask.NameToLayer("UI");
            leftHandCanvas.transform.SetParent(VRSession.Instance.LocalPlayer.Bones.LocalLeftHand, false);
            leftHandCanvas.transform.localPosition = new Vector3(0, 0, 0);
            leftHandCanvas.transform.localRotation = Quaternion.Euler(0, 0, 0);

            rightHandCanvas = new GameObject("Right Hand Canvas").AddComponent<Canvas>();
            rightHandCanvas.worldCamera = VRSession.Instance.MainCamera;
            rightHandCanvas.renderMode = RenderMode.WorldSpace;
            rightHandCanvas.transform.localScale = Vector3.one * 0.001f;
            rightHandCanvas.gameObject.layer = LayerMask.NameToLayer("UI");
            rightHandCanvas.transform.SetParent(VRSession.Instance.LocalPlayer.Bones.LocalRightHand, false);
            rightHandCanvas.transform.localPosition = new Vector3(0, 0, 0);
            rightHandCanvas.transform.localRotation = Quaternion.Euler(0, 0, 0);
        }

        var canvas = gameObject.AddComponent<Canvas>();
        canvas.worldCamera = VRSession.Instance.MainCamera;
        canvas.renderMode = RenderMode.WorldSpace;

        // Flat Canvas: Add tracked graphic raycaster
        GameObject.Find("Systems").Find("UI/Canvas").AddComponent<TrackedDeviceGraphicRaycaster>();

        // Pause menu: Move a little forward
        var quickMenu = GameObject.Find("Systems").Find("UI/Canvas/QuickMenu").transform;
        quickMenu.localPosition = new Vector3(0, 0, -50);

        // Object scanner: Custom handler
        var objectScanner = GameObject.Find("ObjectScanner");
        objectScanner.transform.parent = null;

        var globalScanInfo = GameObject.Find("GlobalScanInfo");

        globalScanInfo.transform.SetParent(transform, false);
        globalScanInfo.transform.localPosition = Vector3.zero;
        globalScanInfo.transform.localRotation = Quaternion.identity;
        globalScanInfo.transform.localScale = Vector3.one;

        scanner = new ObjectScanner();

        // Player cursor: Attach to world interaction canvas

        var cursor = GameObject.Find("PlayerCursor");
        cursor.transform.SetParent(worldInteractionCanvas.transform, false);

        foreach (var image in cursor.GetComponentsInChildren<Image>())
        {
            image.material = AssetManager.alwaysOnTopMat;
        }

        foreach (var text in cursor.GetComponentsInChildren<TextMeshProUGUI>())
        {
            text.fontSharedMaterial = new Material(text.fontSharedMaterial);
            text.isOverlay = true;
        }

        // SelfRed, Self, SprintMeter, RedGlowBodyParts, WeightUI: Attach to left hand (unless disabled)
        selfRed = GameObject.Find("SelfRed");
        self = GameObject.Find("Self");
        sprintMeter = GameObject.Find("SprintMeter");
        redGlowBodyParts = GameObject.Find("RedGlowBodyParts");
        weightUi = GameObject.Find("WeightUI");

        if (Plugin.Config.DisableArmHUD.Value)
        {
            selfRed.transform.SetParent(transform, false);
            self.transform.SetParent(transform, false);
            sprintMeter.transform.SetParent(transform, false);
            redGlowBodyParts.transform.SetParent(transform, false);
            weightUi.transform.SetParent(transform, false);

            selfRed.transform.localPosition =
                self.transform.localPosition =
                redGlowBodyParts.transform.localPosition = new Vector3(-284 + xOffset, 114 + yOffset, 0);
            sprintMeter.transform.localPosition = new Vector3(-284 + xOffset, 80 + yOffset, 0);
            weightUi.transform.localPosition = new Vector3(-195 + xOffset, 25 + yOffset, 0);

            selfRed.transform.localRotation =
                self.transform.localRotation =
                sprintMeter.transform.localRotation =
                redGlowBodyParts.transform.localRotation =
                weightUi.transform.localRotation = Quaternion.identity;

            selfRed.transform.localScale =
                self.transform.localScale =
                sprintMeter.transform.localScale =
                redGlowBodyParts.transform.localScale = Vector3.one * 2;
            weightUi.transform.localScale = Vector3.one;

            weightUi.transform.Find("Weight").localScale = Vector3.one * 1.4f;
        }
        else
        {
            // Self, SelfRed, RedGlowBodyParts = Pos (8, 112, 40) Rot (0 164 0)
            // SprintMeter = Pos (4, 100, 40) Rot (0 164 0)
            // WeightUI = Pos (-10 80 40) Rot (0 164 0)
            
            selfRed.transform.SetParent(leftHandCanvas.transform, false);
            self.transform.SetParent(leftHandCanvas.transform, false);
            sprintMeter.transform.SetParent(leftHandCanvas.transform, false);
            redGlowBodyParts.transform.SetParent(leftHandCanvas.transform, false);
            weightUi.transform.SetParent(leftHandCanvas.transform, false);

            selfRed.transform.localPosition =
                self.transform.localPosition =
                redGlowBodyParts.transform.localPosition = new Vector3(-50, 114, 75);
            sprintMeter.transform.localPosition = new Vector3(-50, 100, 72);
            weightUi.transform.localPosition = new Vector3(-50, 80, 65);

            // idk what the official formatting rule is for this kind of code but I guess this looks fine
            selfRed.transform.localRotation =
                self.transform.localRotation =
                sprintMeter.transform.localRotation =
                redGlowBodyParts.transform.localRotation =
                weightUi.transform.localRotation = Quaternion.Euler(0, 90, 0);

            selfRed.transform.localScale =
                self.transform.localScale =
                sprintMeter.transform.localScale =
                redGlowBodyParts.transform.localScale =
                weightUi.transform.localScale = Vector3.one;

            weightUi.transform.Find("Weight").localScale = Vector3.one * 0.7f;
        }

        // Clock: Attach to left hand
        clock = GameObject.Find("ProfitQuota");

        if (Plugin.Config.DisableArmHUD.Value)
        {
            clock.transform.SetParent(transform, false);
            clock.transform.localPosition = new Vector3(xOffset, yOffset, 0);
            clock.transform.localRotation = Quaternion.identity;
            clock.transform.localScale = Vector3.one;
        }
        else
        {
            clock.transform.SetParent(leftHandCanvas.transform, false);
            clock.transform.localPosition = new Vector3(-2, -46, 64);
            clock.transform.localRotation = Quaternion.Euler(0, 164, 0);
            clock.transform.localScale = Vector3.one * 0.7f;
        }

        // Battery: Attach to right hand (next to knuckles)
        battery = GameObject.Find("Batteries");

        if (Plugin.Config.DisableArmHUD.Value)
        {
            battery.transform.SetParent(transform, false);
            battery.transform.localPosition = new Vector3(-324 + xOffset, 164 + yOffset, 0);
            battery.transform.localRotation = Quaternion.identity;
            battery.transform.localScale = Vector3.one * 2;

            var icon = battery.transform.Find("BatteryIcon");

            icon.localPosition = new Vector3(-16, 16, 0);
            icon.localRotation = Quaternion.identity;
            icon.localScale = Vector3.one * 0.5f;
        }
        else
        {
            battery.transform.SetParent(rightHandCanvas.transform, false);
            battery.transform.localPosition = new Vector3(12, 130, 40);
            battery.transform.localRotation = Quaternion.Euler(0, 195, -35);
            battery.transform.localScale = Vector3.one * 2;

            battery.transform.Find("BatteryIcon").gameObject.SetActive(false);
        }

        var batteryMeter = battery.transform.Find("BatteryMeter");
        batteryMeter.localPosition = Vector3.zero;
        batteryMeter.localRotation = Quaternion.identity;
        batteryMeter.localScale = Vector3.one;

        // Inventory: Attach to right hand (below knuckles)
        inventory = GameObject.Find("Inventory");

        if (Plugin.Config.DisableArmHUD.Value)
        {
            inventory.transform.SetParent(transform, false);
            inventory.transform.localPosition = new Vector3(91 + xOffset, -185 + yOffset, 0);
            inventory.transform.localRotation = Quaternion.identity;
        }
        else
        {
            inventory.transform.SetParent(rightHandCanvas.transform, false);
            inventory.transform.localPosition = new Vector3(-28, 120, 40);
            inventory.transform.localRotation = Quaternion.Euler(0, 195, 0);
            inventory.transform.localScale = Vector3.one * 0.8f;
        }

        // Special HUD: In front of eyes
        var specialHud = GameObject.Find("SpecialHUDGraphics");

        specialHud.transform.SetParent(transform, false);
        specialHud.transform.localPosition = Vector3.zero;
        specialHud.transform.localRotation = Quaternion.identity;
        specialHud.transform.localScale = Vector3.one;

        var hintPanel = GameObject.Find("HintPanelContainer");

        hintPanel.transform.localPosition = new Vector3(0, -17, 8);
        hintPanel.transform.localRotation = Quaternion.identity;
        hintPanel.transform.localScale = Vector3.one;

        var globalNotification = GameObject.Find("GlobalNotification");

        globalNotification.transform.localPosition = new Vector3(-188, -72, 8);
        globalNotification.transform.localScale = Vector3.one;

        // Special Graphics: In front of eyes
        var specialGraphics = GameObject.Find("SpecialGraphics");

        specialGraphics.transform.SetParent(transform, false);
        specialGraphics.transform.localPosition = Vector3.zero;
        specialGraphics.transform.localRotation = Quaternion.identity;
        specialGraphics.transform.localScale = Vector3.one;

        specialGraphics.Find("SinkingUnderCover").SetActive(false);
        specialGraphics.Find("ScrapItemInfo").transform.localPosition = new Vector3(-90, -6, 0);

        // Cinematic Graphics (Planet description)
        var cinematicGraphics = GameObject.Find("CinematicGraphics");

        cinematicGraphics.transform.SetParent(transform, false);
        cinematicGraphics.transform.localPosition = new Vector3(-270, -200, 0);
        cinematicGraphics.transform.localRotation = Quaternion.Euler(0, -9.3337f, 0);
        cinematicGraphics.transform.localScale = Vector3.one;

        // Dialogue Box: In front of eyes
        var dialogueBox = GameObject.Find("DialogueBox").transform;

        dialogueBox.SetParent(transform, false);
        dialogueBox.localPosition = Vector3.zero;
        dialogueBox.localRotation = Quaternion.identity;
        dialogueBox.localScale = Vector3.one;

        // Endgame Stats: In front of eyes
        var endgameStats = GameObject.Find("EndgameStats").transform;
        var endgameStatsContainer = new GameObject("EndgameStatsScaleContainer").transform;

        endgameStatsContainer.SetParent(transform, false);
        endgameStatsContainer.localPosition = Vector3.zero;
        endgameStatsContainer.localRotation = Quaternion.identity;
        endgameStatsContainer.localScale = Vector3.one * 1.4f;

        endgameStats.SetParent(endgameStatsContainer, false);
        endgameStats.localPosition = Vector3.zero;
        endgameStats.localRotation = Quaternion.identity;

        // Loading Screen: In front of eyes
        var loadingScreen = GameObject.Find("LoadingText");

        loadingScreen.transform.SetParent(transform, false);
        loadingScreen.transform.localPosition = Vector3.zero;
        loadingScreen.transform.localRotation = Quaternion.identity;
        loadingScreen.transform.localScale = Vector3.one;

        var darkenScreen = GameObject.Find("DarkenScreen");

        darkenScreen.transform.localScale = Vector3.one * 18;

        // Fired screen: In front of eyes
        var firedScreen = GameObject.Find("GameOverScreen");

        firedScreen.transform.SetParent(transform, false);
        firedScreen.transform.localPosition = Vector3.zero;
        firedScreen.transform.localRotation = Quaternion.identity;
        firedScreen.transform.localScale = Vector3.one;

        firedScreen.transform.Find("DarkenScreen (1)").localScale = Vector3.one * 1.5f;
        firedScreen.transform.Find("DarkenScreen (2)").localScale = Vector3.one * 5;

        // Death/spectator screen: In front of eyes
        deathScreen = GameObject.Find("Systems/UI/Canvas/DeathScreen");

        deathScreen.transform.SetParent(transform, false);
        deathScreen.transform.localPosition = Vector3.zero;
        deathScreen.transform.localEulerAngles = Vector3.zero;
        deathScreen.transform.localScale = Vector3.one * 1.1f;

        // Player screen (Render texture): World space
        spectateCanvas = GameObject.Find("Systems/UI/Canvas").GetComponent<Canvas>();
        spectateCanvas.worldCamera = GameObject.Find("UICamera").GetComponent<Camera>();
        spectateCanvas.renderMode = RenderMode.WorldSpace;
        spectateCanvas.transform.position = new Vector3(0, -999, 0);

        var follow = spectateCanvas.gameObject.AddComponent<CanvasTransformFollow>();
        follow.sourceTransform = VRSession.Instance.UICamera.transform;
        follow.heightOffset = -999;

        transform.localScale = Vector3.one * 0.0007f;

        InitializeKeyboard();

        // Set up a global light for spectators to be able to toggle
        spectatorLight = Instantiate(AssetManager.spectatorLight, transform);
        spectatorLight.SetActive(false);
    }

    private void LateUpdate()
    {
        var camTransform = VRSession.Instance.MainCamera.transform;
        
        transform.position = camTransform.position + camTransform.forward * 0.5f;
        transform.rotation = camTransform.rotation;

        // Interaction canvas
        worldInteractionCanvas.transform.rotation = Quaternion.LookRotation(worldInteractionCanvas.transform.position - camTransform.position);
        worldInteractionCanvas.transform.position += worldInteractionCanvas.transform.forward * -0.2f;

        scanner.Update();
    }

    public void UpdateInteractCanvasPosition(Vector3 position)
    {
        worldInteractionCanvas.transform.position = position;
    }

    public void HideHUD(bool hide)
    {
        selfRed.SetActive(!hide);
        self.SetActive(!hide);
        sprintMeter.SetActive(!hide);
        redGlowBodyParts.SetActive(!hide);
        weightUi.SetActive(!hide);
        
        // Keep clock UI for spectators to be able to see the time
        
        battery.SetActive(!hide);
        inventory.SetActive(!hide);
    }

    public void ToggleDeathScreen(bool? enabled = null)
    {
        if (!deathScreen)
            return;

        if (enabled != null)
        {
            deathScreen.transform.localScale = Vector3.one * (enabled == true ? 1.1f : 0f);
            return;
        }

        if (deathScreen.transform.localScale == Vector3.one * 1.1f)
            deathScreen.transform.localScale = Vector3.zero;
        else
            deathScreen.transform.localScale = Vector3.one * 1.1f;
    }

    public void ToggleSpectatorLight(bool? enabled = null)
    {
        spectatorLight?.SetActive(enabled ?? !spectatorLight.activeSelf);
    }

    /// <summary>
    /// Add a keyboard to the pause menu
    /// </summary>
    private void InitializeKeyboard()
    {
        var canvas = GameObject.Find("Systems/UI/Canvas").GetComponent<Canvas>();
        menuKeyboard = Instantiate(AssetManager.keyboard).GetComponent<NonNativeKeyboard>();

        menuKeyboard.transform.SetParent(canvas.transform, false);
        menuKeyboard.transform.localPosition = new Vector3(0, -470, -40);
        menuKeyboard.transform.localEulerAngles = new Vector3(13, 0, 0);
        menuKeyboard.transform.localScale = Vector3.one * 0.8f;

        menuKeyboard.gameObject.Find("keyboard_Alpha/Deny_Button").SetActive(false);
        menuKeyboard.gameObject.Find("keyboard_Alpha/Confirm_Button").SetActive(false);

        var component = canvas.gameObject.AddComponent<Keyboard>();
        component.keyboard = menuKeyboard;
    }
}
