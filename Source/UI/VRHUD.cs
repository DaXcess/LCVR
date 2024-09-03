using System.Collections.Generic;
using LCVR.Assets;
using LCVR.Player;
using Microsoft.MixedReality.Toolkit.Experimental.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

// ReSharper disable MemberCanBePrivate.Global

namespace LCVR.UI;

public class VRHUD : MonoBehaviour
{
    // Cache for storing Materials for setting UI to be always on top
    public static readonly Dictionary<Material, Material> materialMappings = [];
    
    private GameObject selfRed;
    private GameObject self;
    private GameObject sprintMeter;
    private GameObject redGlowBodyParts;
    private GameObject weightUi;
    private GameObject pttIcon;
    private GameObject sprintIcon;
    private GameObject clock;
    private GameObject battery;
    private GameObject inventory;
    private GameObject deathScreen;

    private GameObject spectatorLight;
    
    /// <summary>
    /// The "Face" canvas is a canvas that simulates a screen-space canvas by always being stuck in front of the camera,
    /// regardless of camera rotation
    /// </summary>
    public Canvas FaceCanvas { get; private set; }
    
    /// <summary>
    /// The pitch-locked canvas is a canvas that only smoothly rotates on the Y axis, which might be more pleasant for
    /// some users
    /// </summary>
    public Canvas PitchLockedCanvas { get; private set; }
    
    /// <summary>
    /// The world interaction canvas is the canvas used for showing tooltips and icons on interactable items
    /// </summary>
    public Canvas WorldInteractionCanvas { get; private set; }
    
    /// <summary>
    /// The pause menu screen, which is strategically placed very far below the map as to not interfere with anything.
    /// </summary>
    public Canvas PauseMenuCanvas { get; private set; }
    
    /// <summary>
    /// The canvas used for displaying UI elements on or near the left hand
    /// </summary>
    public Canvas LeftHandCanvas { get; private set; }
    
    /// <summary>
    /// The canvas used for displaying UI elements on or near the right hand
    /// </summary>
    public Canvas RightHandCanvas { get; private set; }

    /// <summary>
    /// The keyboard that is used within the pause menu
    /// </summary>
    public NonNativeKeyboard MenuKeyboard { get; private set; }
    
    /// <summary>
    /// The keyboard that is used within the Terminal
    /// </summary>
    public NonNativeKeyboard TerminalKeyboard { get; internal set; }
    
    /// <summary>
    /// The sprint icon used for toggle sprint
    /// </summary>
    public Image SprintIcon { get; private set; }
    
    private void Awake()
    {
        // Create canvasses
        WorldInteractionCanvas = new GameObject("World Interaction Canvas")
        {
            transform =
            {
                localScale = Vector3.one * 0.0066f
            }
        }.AddComponent<Canvas>();
        WorldInteractionCanvas.worldCamera = VRSession.Instance.MainCamera;
        WorldInteractionCanvas.renderMode = RenderMode.WorldSpace;
        WorldInteractionCanvas.sortingOrder = 1;

        FaceCanvas = new GameObject("Face VR Canvas")
        {
            transform =
            {
                parent = transform,
                localScale = Vector3.one * 0.0007f
            }
        }.AddComponent<Canvas>();
        FaceCanvas.worldCamera = VRSession.Instance.MainCamera;
        FaceCanvas.renderMode = RenderMode.WorldSpace;
        FaceCanvas.sortingOrder = 1;
        
        PitchLockedCanvas = new GameObject("Pitch Locked VR Canvas")
        {
            transform =
            {
                parent = transform,
                localScale = Vector3.one * 0.0007f
            }
        }.AddComponent<Canvas>();
        PitchLockedCanvas.worldCamera = VRSession.Instance.MainCamera;
        PitchLockedCanvas.renderMode = RenderMode.WorldSpace;
        PitchLockedCanvas.sortingOrder = 1;
        
        var xOffset = Plugin.Config.HUDOffsetX.Value;
        var yOffset = Plugin.Config.HUDOffsetY.Value;

        if (!Plugin.Config.DisableArmHUD.Value)
        {
            LeftHandCanvas = new GameObject("Left Hand Canvas").AddComponent<Canvas>();
            LeftHandCanvas.worldCamera = VRSession.Instance.MainCamera;
            LeftHandCanvas.renderMode = RenderMode.WorldSpace;
            LeftHandCanvas.transform.localScale = Vector3.one * 0.001f;
            LeftHandCanvas.gameObject.layer = LayerMask.NameToLayer("UI");
            LeftHandCanvas.transform.SetParent(VRSession.Instance.LocalPlayer.Bones.LocalLeftHand, false);
            LeftHandCanvas.transform.localPosition = new Vector3(0, 0, 0);
            LeftHandCanvas.transform.localRotation = Quaternion.Euler(0, 0, 0);

            RightHandCanvas = new GameObject("Right Hand Canvas").AddComponent<Canvas>();
            RightHandCanvas.worldCamera = VRSession.Instance.MainCamera;
            RightHandCanvas.renderMode = RenderMode.WorldSpace;
            RightHandCanvas.transform.localScale = Vector3.one * 0.001f;
            RightHandCanvas.gameObject.layer = LayerMask.NameToLayer("UI");
            RightHandCanvas.transform.SetParent(VRSession.Instance.LocalPlayer.Bones.LocalRightHand, false);
            RightHandCanvas.transform.localPosition = new Vector3(0, 0, 0);
            RightHandCanvas.transform.localRotation = Quaternion.Euler(0, 0, 0);
        }
        
        // Flat Canvas: Add tracked graphic raycaster
        GameObject.Find("Systems").Find("UI/Canvas").AddComponent<TrackedDeviceGraphicRaycaster>();

        // Pause menu: Move a little forward
        var quickMenu = GameObject.Find("Systems").Find("UI/Canvas/QuickMenu").transform;
        quickMenu.localPosition = new Vector3(0, 0, -50);

        // Object scanner: Custom handler
        var objectScanner = GameObject.Find("ObjectScanner");
        objectScanner.transform.parent = null;

        var globalScanInfo = GameObject.Find("GlobalScanInfo");

        globalScanInfo.transform.SetParent(PitchLockedCanvas.transform, false);
        globalScanInfo.transform.localPosition =
            Plugin.Config.EnablePitchLockedCanvas.Value ? Vector3.down * 150 : Vector3.zero;
        globalScanInfo.transform.localRotation = Quaternion.identity;
        globalScanInfo.transform.localScale = Vector3.one * (Plugin.Config.EnablePitchLockedCanvas.Value ? 1.2f : 1);

        gameObject.AddComponent<ObjectScanner>();

        // Player cursor: Attach to world interaction canvas

        var cursor = GameObject.Find("PlayerCursor");
        cursor.transform.SetParent(WorldInteractionCanvas.transform, false);

        // SelfRed, Self, SprintMeter, RedGlowBodyParts, WeightUI, PTT: Attach to left hand (unless disabled)
        selfRed = GameObject.Find("SelfRed");
        self = GameObject.Find("Self");
        sprintMeter = GameObject.Find("SprintMeter");
        redGlowBodyParts = GameObject.Find("RedGlowBodyParts");
        weightUi = GameObject.Find("WeightUI");
        pttIcon = GameObject.Find("PTTIcon");
        
        sprintIcon = Instantiate(pttIcon, pttIcon.transform.parent);
        sprintIcon.name = "SprintIcon";
        sprintIcon.GetComponent<Image>().sprite = AssetManager.SprintImage;

        SprintIcon = sprintIcon.GetComponent<Image>();
        
        if (Plugin.Config.DisableArmHUD.Value)
        {
            selfRed.transform.SetParent(FaceCanvas.transform, false);
            self.transform.SetParent(FaceCanvas.transform, false);
            sprintMeter.transform.SetParent(FaceCanvas.transform, false);
            redGlowBodyParts.transform.SetParent(FaceCanvas.transform, false);
            weightUi.transform.SetParent(FaceCanvas.transform, false);
            pttIcon.transform.SetParent(FaceCanvas.transform, false);
            sprintIcon.transform.SetParent(FaceCanvas.transform, false);

            selfRed.transform.localPosition =
                self.transform.localPosition =
                    redGlowBodyParts.transform.localPosition = new Vector3(-284 + xOffset, 114 + yOffset, 0);
            sprintMeter.transform.localPosition = new Vector3(-284 + xOffset, 80 + yOffset, 0);
            weightUi.transform.localPosition = new Vector3(-195 + xOffset, 25 + yOffset, 0);
            pttIcon.transform.localPosition = new Vector3(-195 + xOffset, 165 + yOffset, 0);
            sprintIcon.transform.localPosition = new Vector3(-175 + xOffset, 115 + yOffset, 0);

            selfRed.transform.localRotation =
                self.transform.localRotation =
                    sprintMeter.transform.localRotation =
                        redGlowBodyParts.transform.localRotation =
                            weightUi.transform.localRotation =
                                pttIcon.transform.localRotation =
                                    sprintIcon.transform.localRotation = Quaternion.identity;

            selfRed.transform.localScale =
                self.transform.localScale =
                    sprintMeter.transform.localScale =
                        redGlowBodyParts.transform.localScale = 
                            pttIcon.transform.localScale = 
                                sprintIcon.transform.localScale = Vector3.one * 2;

            weightUi.transform.localScale = Vector3.one;
            weightUi.transform.Find("Weight").localScale = Vector3.one * 1.4f;
        }
        else
        {
            // Self, SelfRed, RedGlowBodyParts = Pos (8, 112, 40) Rot (0 164 0)
            // SprintMeter = Pos (4, 100, 40) Rot (0 164 0)
            // WeightUI = Pos (-10 80 40) Rot (0 164 0)

            selfRed.transform.SetParent(LeftHandCanvas.transform, false);
            self.transform.SetParent(LeftHandCanvas.transform, false);
            sprintMeter.transform.SetParent(LeftHandCanvas.transform, false);
            redGlowBodyParts.transform.SetParent(LeftHandCanvas.transform, false);
            weightUi.transform.SetParent(LeftHandCanvas.transform, false);
            pttIcon.transform.SetParent(LeftHandCanvas.transform, false);
            sprintIcon.transform.SetParent(LeftHandCanvas.transform, false);

            selfRed.transform.localPosition =
                self.transform.localPosition =
                    redGlowBodyParts.transform.localPosition = new Vector3(-50, 114, 75);
            sprintMeter.transform.localPosition = new Vector3(-50, 100, 72);
            weightUi.transform.localPosition = new Vector3(-50, 80, 65);
            pttIcon.transform.localPosition = new Vector3(-50, 145, 35);
            sprintIcon.transform.localPosition = new Vector3(-50, 124, 22);

            selfRed.transform.localRotation =
                self.transform.localRotation =
                    sprintMeter.transform.localRotation =
                        redGlowBodyParts.transform.localRotation =
                            weightUi.transform.localRotation =
                                pttIcon.transform.localRotation = 
                                    sprintIcon.transform.localRotation = Quaternion.Euler(0, 90, 0);

            selfRed.transform.localScale =
                self.transform.localScale =
                    sprintMeter.transform.localScale =
                        redGlowBodyParts.transform.localScale =
                            weightUi.transform.localScale =
                                pttIcon.transform.localScale = 
                                    sprintIcon.transform.localScale = Vector3.one;

            weightUi.transform.Find("Weight").localScale = Vector3.one * 0.7f;
        }

        // Clock: Attach to left hand
        clock = GameObject.Find("ProfitQuota");

        if (Plugin.Config.DisableArmHUD.Value)
        {
            clock.transform.SetParent(FaceCanvas.transform, false);
            clock.transform.localPosition = new Vector3(xOffset, yOffset, 0);
            clock.transform.localRotation = Quaternion.identity;
            clock.transform.localScale = Vector3.one;
        }
        else
        {
            clock.transform.SetParent(LeftHandCanvas.transform, false);
            clock.transform.localPosition = new Vector3(-2, -46, 64);
            clock.transform.localRotation = Quaternion.Euler(0, 164, 0);
            clock.transform.localScale = Vector3.one * 0.7f;
        }

        // Battery: Attach to right hand (next to knuckles)
        battery = GameObject.Find("Batteries");

        if (Plugin.Config.DisableArmHUD.Value)
        {
            battery.transform.SetParent(FaceCanvas.transform, false);
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
            battery.transform.SetParent(RightHandCanvas.transform, false);
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
            inventory.transform.SetParent(FaceCanvas.transform, false);
            inventory.transform.localPosition = new Vector3(91 + xOffset, -185 + yOffset, 0);
            inventory.transform.localRotation = Quaternion.identity;
        }
        else
        {
            inventory.transform.SetParent(RightHandCanvas.transform, false);
            inventory.transform.localPosition = new Vector3(-28, 120, 40);
            inventory.transform.localRotation = Quaternion.Euler(0, 195, 0);
            inventory.transform.localScale = Vector3.one * 0.8f;
        }

        // Special HUD: In front of eyes
        var specialHud = GameObject.Find("SpecialHUDGraphics");

        specialHud.transform.SetParent(PitchLockedCanvas.transform, false);
        specialHud.transform.localPosition = Vector3.zero;
        specialHud.transform.localRotation = Quaternion.identity;
        specialHud.transform.localScale = Vector3.one;

        var hintPanel = GameObject.Find("HintPanelContainer");

        hintPanel.transform.localPosition = Plugin.Config.EnablePitchLockedCanvas.Value
            ? new Vector3(0, -375, 8)
            : new Vector3(0, -17, 8);
        hintPanel.transform.localRotation = Quaternion.identity;
        hintPanel.transform.localScale = Vector3.one;

        var globalNotification = GameObject.Find("GlobalNotification");

        globalNotification.transform.localPosition = Plugin.Config.EnablePitchLockedCanvas.Value
            ? new Vector3(-188, -375, 8)
            : new Vector3(-188, -72, 8);
        globalNotification.transform.localScale = Vector3.one;

        // Special Graphics: In front of eyes
        var specialGraphics = GameObject.Find("SpecialGraphics");

        specialGraphics.transform.SetParent(PitchLockedCanvas.transform, false);
        specialGraphics.transform.localPosition = Vector3.zero;
        specialGraphics.transform.localRotation = Quaternion.identity;
        specialGraphics.transform.localScale = Vector3.one;

        specialGraphics.Find("SinkingUnderCover").SetActive(false);
        specialGraphics.Find("ScrapItemInfo").transform.localPosition = new Vector3(-90, -6, 0);
        specialGraphics.Find("SystemNotification").transform.localPosition =
            Plugin.Config.EnablePitchLockedCanvas.Value ? Vector3.down * 250 : Vector3.zero;

        // Cinematic Graphics (Planet description)
        var cinematicGraphics = GameObject.Find("CinematicGraphics");

        cinematicGraphics.transform.SetParent(PitchLockedCanvas.transform, false);
        cinematicGraphics.transform.localPosition = new Vector3(-270, -200, 0);
        cinematicGraphics.transform.localRotation = Quaternion.Euler(0, -9.3337f, 0);
        cinematicGraphics.transform.localScale = Vector3.one;

        // Dialogue Box: In front of eyes
        var dialogueBox = GameObject.Find("DialogueBox").transform;

        dialogueBox.SetParent(PitchLockedCanvas.transform, false);
        dialogueBox.localPosition = Plugin.Config.EnablePitchLockedCanvas.Value ? Vector3.down * 250 : Vector3.zero;
        dialogueBox.localRotation = Quaternion.identity;
        dialogueBox.localScale = Vector3.one * 1.5f;

        // Endgame Stats: In front of eyes
        var endgameStats = GameObject.Find("EndgameStats").transform;
        var endgameStatsContainer = new GameObject("EndgameStatsScaleContainer").transform;

        endgameStatsContainer.SetParent(PitchLockedCanvas.transform, false);
        endgameStatsContainer.localPosition = Vector3.zero;
        endgameStatsContainer.localRotation = Quaternion.identity;
        endgameStatsContainer.localScale = Vector3.one * 1.4f;

        endgameStats.SetParent(endgameStatsContainer, false);
        endgameStats.localPosition = Vector3.zero;
        endgameStats.localRotation = Quaternion.identity;
        
        // Meteor Shower Alert
        var meteorShowerContainer = specialHud.transform.Find("MeteorShowerWarning");
        meteorShowerContainer.Find("Image/Image").gameObject.SetActive(false); // Remove BG
        meteorShowerContainer.localScale = Vector3.one * 0.8f;
        meteorShowerContainer.localPosition = Vector3.down * 100;
        
        // Loading Screen: In front of eyes
        var loadingScreen = GameObject.Find("LoadingText");

        loadingScreen.transform.SetParent(FaceCanvas.transform, false);
        loadingScreen.transform.localPosition = Vector3.zero;
        loadingScreen.transform.localRotation = Quaternion.identity;
        loadingScreen.transform.localScale = Vector3.one;

        var darkenScreen = GameObject.Find("DarkenScreen");

        darkenScreen.transform.localScale = Vector3.one * 18;

        // Fired screen: In front of eyes
        var firedScreen = GameObject.Find("GameOverScreen");

        firedScreen.transform.SetParent(FaceCanvas.transform, false);
        firedScreen.transform.localPosition = Vector3.zero;
        firedScreen.transform.localRotation = Quaternion.identity;
        firedScreen.transform.localScale = Vector3.one;

        firedScreen.transform.Find("DarkenScreen (1)").localScale = Vector3.one * 1.5f;
        firedScreen.transform.Find("DarkenScreen (2)").localScale = Vector3.one * 5;

        // Death/spectator screen: In front of eyes
        deathScreen = GameObject.Find("Systems/UI/Canvas/DeathScreen");

        deathScreen.transform.SetParent(PitchLockedCanvas.transform, false);
        deathScreen.transform.localPosition =
            Plugin.Config.EnablePitchLockedCanvas.Value ? Vector3.up * 50 : Vector3.zero;
        deathScreen.transform.localEulerAngles = Vector3.zero;
        deathScreen.transform.localScale = Vector3.one * 1.1f;
        
        // Systems online: In front of eyes
        var ingamePlayerHud = GameObject.Find("IngamePlayerHUD");
        var systemsOnline = ingamePlayerHud.transform.Find("BottomMiddle/SystemsOnline");
        
        systemsOnline.SetParent(FaceCanvas.transform, false);
        systemsOnline.localPosition = new Vector3(-280, -100, 0);
        systemsOnline.localEulerAngles = Vector3.zero;
        systemsOnline.localScale = Vector3.one * 1.65f;

        // Pause menu screen (Render texture): World space
        PauseMenuCanvas = GameObject.Find("Systems/UI/Canvas").GetComponent<Canvas>();
        PauseMenuCanvas.worldCamera = GameObject.Find("UICamera").GetComponent<Camera>();
        PauseMenuCanvas.renderMode = RenderMode.WorldSpace;
        PauseMenuCanvas.transform.position = new Vector3(0, -999, 0);
        
        var follow = PauseMenuCanvas.gameObject.AddComponent<CanvasTransformFollow>();
        follow.sourceTransform = VRSession.Instance.UICamera.transform;
        follow.heightOffset = -999;
            
        InitializeKeyboard();

        // Set up a global light for spectators to be able to toggle
        spectatorLight = Instantiate(AssetManager.SpectatorLight, transform);
        spectatorLight.SetActive(false);
        
        // Prevents CullFactory from culling the light
        spectatorLight.hideFlags |= HideFlags.DontSave;
        
        MoveToFront(FaceCanvas);
        MoveToFront(PitchLockedCanvas);
        MoveToFront(WorldInteractionCanvas);
        MoveToFront(objectScanner.transform);
        
        // Set up belt bag UI
        FindObjectOfType<BeltBagInventoryUI>(true).gameObject.AddComponent<BeltBagUI>();
    }

    private static void MoveToFront(Component component)
    {
        foreach (var element in component.GetComponentsInChildren<Image>(true))
        {
            if (element.materialForRendering == null)
                continue;

            if (!materialMappings.TryGetValue(element.materialForRendering, out var materialCopy))
            {
                materialCopy = new Material(element.materialForRendering);
                materialMappings.Add(element.materialForRendering, materialCopy);
            }
            
            materialCopy.SetInt("unity_GUIZTestMode", (int)CompareFunction.Always);
            element.material = materialCopy;
        }
        
        foreach (var shit in component.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            shit.m_fontMaterial = shit.CreateMaterialInstance(shit.m_sharedMaterial);
            shit.m_sharedMaterial = shit.m_fontMaterial;
            shit.m_sharedMaterial.shader = AssetManager.TMPAlwaysOnTop;
        }
    }

    private void LateUpdate()
    {
        var camTransform = VRSession.Instance.MainCamera.transform;

        transform.position = camTransform.position;

        // Face canvas        
        FaceCanvas.transform.localPosition =
            Vector3.Lerp(FaceCanvas.transform.localPosition, camTransform.forward * 0.5f, 0.4f);
        FaceCanvas.transform.rotation = Quaternion.Slerp(FaceCanvas.transform.rotation, camTransform.rotation, 0.4f);
        
        // Interaction canvas
        WorldInteractionCanvas.transform.rotation =
            Quaternion.LookRotation(WorldInteractionCanvas.transform.position - camTransform.position);
        WorldInteractionCanvas.transform.position += WorldInteractionCanvas.transform.forward * -0.2f;

        // Pitch locked canvas
        if (Plugin.Config.EnablePitchLockedCanvas.Value)
        {
            var fwd = new Vector3(camTransform.forward.x, 0, camTransform.forward.z).normalized * 0.5f;
            var rot = Quaternion.Euler(0, camTransform.rotation.eulerAngles.y, 0);

            PitchLockedCanvas.transform.localPosition =
                Vector3.Lerp(PitchLockedCanvas.transform.localPosition, fwd, 0.1f);
            PitchLockedCanvas.transform.rotation = Quaternion.Slerp(PitchLockedCanvas.transform.rotation, rot, 0.1f);
        }
        else
        {
            // If disabled, behave like FaceCanvas
            
            PitchLockedCanvas.transform.localPosition = camTransform.forward * 0.5f;
            PitchLockedCanvas.transform.eulerAngles = camTransform.eulerAngles;
        }
    }

    public void UpdateInteractCanvasPosition(Vector3 position)
    {
        WorldInteractionCanvas.transform.position = position;
    }

    public void HideHUD(bool hide)
    {
        selfRed.SetActive(!hide);
        self.SetActive(!hide);
        sprintMeter.SetActive(!hide);
        redGlowBodyParts.SetActive(!hide);
        weightUi.SetActive(!hide);
        pttIcon.SetActive(!hide);
        sprintIcon.SetActive(!hide);
        
        // Keep clock UI for spectators to be able to see the time
        
        battery.SetActive(!hide);
        inventory.SetActive(!hide);
    }

    public void ToggleDeathScreen(bool? visible = null)
    {
        if (!deathScreen)
            return;

        if (visible != null)
        {
            deathScreen.transform.localScale = Vector3.one * (visible == true ? 1.1f : 0f);
            return;
        }

        if (deathScreen.transform.localScale == Vector3.one * 1.1f)
            deathScreen.transform.localScale = Vector3.zero;
        else
            deathScreen.transform.localScale = Vector3.one * 1.1f;
    }

    public void ToggleSpectatorLight(bool? active = null)
    {
        if (spectatorLight is not { } light)
            return;
        
        var hdCamera = VRSession.Instance.MainCamera.GetComponent<HDAdditionalCameraData>();

        // Don't disable volumetrics if it's already disabled, or if the user disabled the feature
        if (!Plugin.Config.DisableVolumetrics.Value && Plugin.Config.SpectatorLightRemovesVolumetrics.Value)
        {
            var enable = active ?? !light.activeSelf;

            if (enable)
                hdCamera.DisableQualitySetting(FrameSettingsField.Volumetrics);
            else
                hdCamera.EnableQualitySetting(FrameSettingsField.Volumetrics);
        }
        
        light.SetActive(active ?? !light.activeSelf);
    }

    /// <summary>
    /// Add a keyboard to the pause menu
    /// </summary>
    private void InitializeKeyboard()
    {
        var canvas = GameObject.Find("Systems/UI/Canvas").GetComponent<Canvas>();
        MenuKeyboard = Instantiate(AssetManager.Keyboard).GetComponent<NonNativeKeyboard>();

        MenuKeyboard.transform.SetParent(canvas.transform, false);
        MenuKeyboard.transform.localPosition = new Vector3(0, -470, -40);
        MenuKeyboard.transform.localEulerAngles = new Vector3(13, 0, 0);
        MenuKeyboard.transform.localScale = Vector3.one * 0.8f;

        MenuKeyboard.gameObject.Find("keyboard_Alpha/Deny_Button").SetActive(false);
        MenuKeyboard.gameObject.Find("keyboard_Alpha/Confirm_Button").SetActive(false);

        var component = canvas.gameObject.AddComponent<Keyboard>();
        component.keyboard = MenuKeyboard;
    }
}
