﻿using HarmonyLib;
using LCVR.Assets;
using LCVR.Input;
using LCVR.Physics.Interactions;
using LCVR.UI;
using Microsoft.MixedReality.Toolkit.Experimental.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.XR;

namespace LCVR.Player;

public class VRSession : MonoBehaviour
{
    public static VRSession Instance { get; private set; }

    /// <summary>
    /// Whether or not the game has VR enabled. This field will only be populated after LCVR has loaded.
    /// </summary>
    public static bool InVR => Plugin.Flags.HasFlag(Flags.VR);

    private VRPlayer localPlayer;
    private VRHUD hud;

    private Camera playerGameplayCamera;
    private Camera menuCamera;

    #region Controllers
    private MotionDetector motionDetector;
    #endregion

    #region Custom Camera
    private bool customCameraEnabled = false;
    private Camera customCamera;
    private float customCameraLerpFactor;
    #endregion

    #region Public Accessors
    public VRPlayer LocalPlayer => localPlayer;
    public VRHUD HUD => hud;

    public Camera MainCamera => playerGameplayCamera;
    public Camera UICamera => menuCamera;

    public MotionDetector MotionDetector => motionDetector;

    public Rendering.VolumeManager VolumeManager { get; private set; }

    public InteractionManager InteractionManager { get; private set; }
    public ShipLever ShipLever { get; private set; }
    public ChargeStation ChargeStation { get; private set; }
    public Muffler Muffler { get; private set; }
    public Face Face { get; private set; }
    #endregion

    void Awake()
    {
        Instance = this;

        playerGameplayCamera = StartOfRound.Instance.activeCamera;
        menuCamera = GameObject.Find("UICamera").GetComponent<Camera>();

        if (InVR)
            InitializeVRSession();

        #region Initialize universal interactions
        ShipLever = ShipLever.Create();
        ChargeStation = ChargeStation.Create();
        #endregion

        // Enable this line to show the interactors on the camera
#if DEBUG && false
        playerGameplayCamera.cullingMask |= 1 << 11;
#endif
    }

    void Update()
    {
        if (customCameraEnabled && !localPlayer.IsDead)
        {
            customCamera.transform.position = playerGameplayCamera.transform.position;
            customCamera.transform.rotation = Quaternion.Lerp(customCamera.transform.rotation, playerGameplayCamera.transform.rotation, customCameraLerpFactor);
        }
    }

    private void InitializeVRSession()
    {
        #region Disable base UI input system
        var input = GameObject.Find("EventSystem")?.GetComponent<InputSystemUIInputModule>();

        if (input != null)
            input.enabled = false;
        #endregion

        #region Set up helmet model
        // Move around the volumetric plane
        var helmetContainer = GameObject.Find("Systems/Rendering/PlayerHUDHelmetModel");
        var helmetModel = helmetContainer.Find("ScavengerHelmet");
        helmetModel.transform.Find("Plane").SetParent(helmetContainer.transform);

        // Toggle helmet visor visibility
        helmetModel.SetActive(Plugin.Config.EnableHelmetVisor.Value);

        // Move helmet model to child of target point
        var helmetTarget = StartOfRound.Instance.localPlayerController.gameObject.Find("ScavengerModel/metarig/CameraContainer/MainCamera/HUDHelmetPosition").transform;
        helmetContainer.transform.SetParent(helmetTarget, false);

        helmetContainer.transform.localPosition = Vector3.zero;
        helmetContainer.transform.localEulerAngles = Vector3.zero;

        helmetTarget.transform.localPosition = new Vector3(0.01f, -0.068f, -0.073f);
        helmetTarget.transform.localScale = Vector3.one;
        #endregion

        #region Disable UI camera and promote FPV camera
        playerGameplayCamera.targetTexture = null;
        menuCamera.GetComponent<HDAdditionalCameraData>().xrRendering = false;
        menuCamera.stereoTargetEye = StereoTargetEyeMask.None;
        menuCamera.enabled = false;

        playerGameplayCamera.stereoTargetEye = StereoTargetEyeMask.Both;
        playerGameplayCamera.GetComponent<HDAdditionalCameraData>().xrRendering = true;

        playerGameplayCamera.depth = menuCamera.depth + 1;

        // Create HMD tracker
        var cameraPoseDriver = playerGameplayCamera.gameObject.AddComponent<TrackedPoseDriver>();
        cameraPoseDriver.positionAction = Actions.Instance.HeadPosition;
        cameraPoseDriver.rotationAction = Actions.Instance.HeadRotation;
        cameraPoseDriver.trackingStateInput = new InputActionProperty(Actions.Instance.HeadTrackingState);
        #endregion

        #region Setup Pause Menu Camera
        var uiCameraAnchor = new GameObject("UI Camera Anchor");

        uiCameraAnchor.transform.position = new Vector3(0, -1000, 0);
        menuCamera.transform.SetParent(uiCameraAnchor.transform, false);
        menuCamera.cullingMask = -1;

        uiCameraAnchor.AddComponent<VRPauseMenu>();

        var uiCameraPoseDriver = menuCamera.gameObject.AddComponent<TrackedPoseDriver>();
        uiCameraPoseDriver.positionAction = Actions.Instance.HeadPosition;
        uiCameraPoseDriver.rotationAction = Actions.Instance.HeadRotation;
        uiCameraPoseDriver.trackingStateInput = new InputActionProperty(Actions.Instance.HeadTrackingState);
        #endregion

        #region Apply optimization configruation
        var hdCamera = playerGameplayCamera.GetComponent<HDAdditionalCameraData>();

        if (Plugin.Config.EnableDLSS.Value)
        {
            hdCamera.allowDynamicResolution = true;
            hdCamera.allowDeepLearningSuperSampling = true;
        }

        hdCamera.DisableQualitySetting(FrameSettingsField.DepthOfField);
        hdCamera.DisableQualitySetting(FrameSettingsField.SSAO);
        hdCamera.DisableQualitySetting(FrameSettingsField.SSAOAsync);

        if (Plugin.Config.DisableVolumetrics.Value)
            hdCamera.DisableQualitySetting(FrameSettingsField.Volumetrics);

        if (!Plugin.Config.CameraResolutionGlobal.Value)
            XRSettings.eyeTextureResolutionScale = Plugin.Config.CameraResolution.Value;

        XRSettings.useOcclusionMesh = false;
        #endregion

        #region Disable lens distortion effects
        var profiles = new VolumeProfile[] {
            HUDManager.Instance.insanityScreenFilter.profile,
        };

        var extendedProfiles = new VolumeProfile[] {
            HUDManager.Instance.drunknessFilter.profile,
            HUDManager.Instance.flashbangScreenFilter.profile,
            HUDManager.Instance.underwaterScreenFilter.profile,
        };

        var distortionFilters = new List<LensDistortion>();

        distortionFilters.AddRange(profiles.SelectMany(profile => profile.components.FindAll(component => component is LensDistortion).Select(component => component as LensDistortion)));

        if (Plugin.Config.DisableLensDistortion.Value)
            distortionFilters.AddRange(extendedProfiles.SelectMany(profile => profile.components.FindAll(component => component is LensDistortion).Select(component => component as LensDistortion)));

        distortionFilters.ForEach(filter => filter.active = false);
        #endregion

        #region Initialize secondary custom camera
        if (Plugin.Config.EnableCustomCamera.Value)
            InitializeCustomCamera();
        #endregion

        #region Add keyboard to Terminal
        var terminal = FindObjectOfType<Terminal>();

        var terminalKeyboardObject = Instantiate(AssetManager.keyboard);
        terminalKeyboardObject.transform.SetParent(terminal.transform.parent.parent, false);
        terminalKeyboardObject.transform.localPosition = new Vector3(-0.584f, 0.333f, 0.791f);
        terminalKeyboardObject.transform.localEulerAngles = new Vector3(0, 90, 90);
        terminalKeyboardObject.transform.localScale = Vector3.one * 0.0009f;

        terminalKeyboardObject.GetComponent<Canvas>().worldCamera = playerGameplayCamera;

        var terminalKeyboard = terminalKeyboardObject.GetComponent<NonNativeKeyboard>();
        terminalKeyboard.InputField = terminal.screenText;
        terminalKeyboard.CloseOnEnter = false;

        terminalKeyboard.OnKeyboardValueKeyPressed += (_) =>
        {
            RoundManager.PlayRandomClip(terminal.terminalAudio, terminal.keyboardClips);
        };

        terminalKeyboard.OnKeyboardFunctionKeyPressed += (_) =>
        {
            RoundManager.PlayRandomClip(terminal.terminalAudio, terminal.keyboardClips);
        };

        terminalKeyboard.OnTextSubmitted += (_, _) =>
        {
            terminal.OnSubmit();
        };

        terminalKeyboard.OnMacroTriggered += (text) =>
        {
            terminal.screenText.text = terminal.screenText.text.Substring(0, terminal.screenText.text.Length - terminal.textAdded);
            terminal.screenText.text += text;
            terminal.textAdded = text.Length;
            terminal.OnSubmit();
        };

        terminalKeyboard.OnClosed += (_, _) =>
        {
            terminal.QuitTerminal();
        };
        #endregion

        #region Set up motion detector
        motionDetector = new GameObject("Motion Detector").AddComponent<MotionDetector>();
        motionDetector.gameObject.CreateInteractorController(Utils.Hand.Right, false, true, false);
        #endregion

        #region Initialize VR Player
        localPlayer = StartOfRound.Instance.localPlayerController.gameObject.AddComponent<VRPlayer>();
        #endregion

        #region Initialize Interaction Manager
        InteractionManager = new GameObject("Interaction Manager").AddComponent<InteractionManager>();
        InteractionManager.transform.parent = transform;
        #endregion

        #region Initialize HUD
        hud = new GameObject("VR HUD").AddComponent<VRHUD>();
        hud.terminalKeyboard = terminalKeyboard;
        #endregion

        #region Initialize VR-Only interactions
        // Creates interactors for both monitor buttons, and also moves the buttons into a more accessible position
        MonitorButton.Create();

        // Creates interactors for both ship door buttons
        ShipDoorButton.Create();

        // Creates interactor for muting yourself using your hand
        Muffler = Muffler.Create();

        // Creates interactor for using items when held up to your face
        Face = Face.Create();

        // Teleporters, the ship horn and doors are not guaranteed to be present when spawning in a lobby,
        // so they are initialized with the help of patches on the "Start" and "Awake" lifetime methods
        #endregion

        #region Apply disable interactions config
        // Ship Lever
        if (!Plugin.Config.DisableShipLeverInteraction.Value)
            VRController.DisableInteractTrigger("StartGameLever");

        // Charging Station
        if (!Plugin.Config.DisableChargeStationInteraction.Value)
            VRController.DisableInteractTrigger("ChargingStationTrigger");

        // Teleporter
        if (!Plugin.Config.DisableTeleporterInteraction.Value)
        {
            VRController.DisableInteractTrigger("ButtonGlass");
            VRController.DisableInteractTrigger("RedButton");
        }

        // Monitor Buttons
        if (!Plugin.Config.DisableMonitorInteraction.Value)
            VRController.DisableInteractTrigger("MonitorButtonInteractable");

        // Ship Door Buttons
        if (!Plugin.Config.DisableShipDoorInteraction.Value)
            VRController.DisableInteractTrigger("ShipDoorButtonInteractable");

        // Company Bell
        if (!Plugin.Config.DisableCompanyBellInteraction.Value)
            VRController.DisableInteractTrigger("CompanyBellTrigger");

        // Ship Horn
        if (!Plugin.Config.DisableShipHornInteraction.Value)
            VRController.DisableInteractTrigger("ShipHornPullInteractable");

        // Breaker Box
        if (!Plugin.Config.DisableBreakerBoxInteraction.Value)
        {
            VRController.DisableInteractTrigger("PowerBoxDoor");

            for (var i = 1; i <= 5; i++)
                VRController.DisableInteractTrigger($"BreakerSwitch{i}");
        }

        // Doors
        if (!Plugin.Config.DisableDoorInteraction.Value)
        {
            VRController.DisableInteractTrigger("DoorInteractable");
            VRController.DisableInteractTrigger("LockPickerInteractable");
        }
        #endregion

#if DEBUG
        Experiments.Experiments.RunExperiments();
#endif

        #region Misc
        VolumeManager = Instantiate(AssetManager.volumeManager, transform).GetComponent<Rendering.VolumeManager>();

        Items.UpdateVRControlsItemsOffsets();
        Actions.Instance.Reload();
        #endregion
    }

    #region VR
    private void InitializeCustomCamera()
    {
        customCameraEnabled = true;
        customCameraLerpFactor = Mathf.Clamp(Plugin.Config.CustomCameraLerpFactor.Value, 0.01f, 1f);

        var children = playerGameplayCamera.transform.GetChildren();

        children.Do(child => child.SetParent(null, true));

        customCamera = Instantiate(playerGameplayCamera);
        customCamera.name = "Custom Camera";
        customCamera.transform.SetParent(playerGameplayCamera.transform.parent, false);
        customCamera.transform.localEulerAngles = Vector3.zero;
        customCamera.transform.localPosition = Vector3.zero;
        customCamera.transform.localScale = Vector3.one;

        customCamera.fieldOfView = Plugin.Config.CustomCameraFOV.Value;
        customCamera.depth++;
        customCamera.stereoTargetEye = StereoTargetEyeMask.None;
        customCamera.targetDisplay = 0;

        var hdDesktopCamera = customCamera.GetComponent<HDAdditionalCameraData>();
        hdDesktopCamera.xrRendering = false;

        children.Do(child => child.SetParent(playerGameplayCamera.transform, true));
    }

    public void OnEnterTerminal()
    {
        HUD.terminalKeyboard.PresentKeyboard();

        LocalPlayer.EnableInteractorVisuals();
        LocalPlayer.PrimaryController.EnableDebugInteractorVisual(false);
    }

    public void OnExitTerminal()
    {
        if (HUD.terminalKeyboard.isActiveAndEnabled)
            HUD.terminalKeyboard.Close();

        LocalPlayer.EnableInteractorVisuals(false);
        LocalPlayer.PrimaryController.EnableDebugInteractorVisual();
    }

    public void OnPauseMenuOpened()
    {
        Logger.LogDebug("Opened pause menu");

        // Make sure keyboard is closed when pause menu opens
        HUD.menuKeyboard.Close();
        SwitchToUICamera();

        LocalPlayer.PrimaryController.enabled = false;
    }

    public void OnPauseMenuClosed()
    {
        Logger.LogDebug("Closed pause menu");

        HUD.menuKeyboard.Close();
        SwitchToGameCamera();

        LocalPlayer.PrimaryController.enabled = true;
    }

    private void SwitchToUICamera()
    {
        var hdUICamera = menuCamera.GetComponent<HDAdditionalCameraData>();
        var hdMainCamera = playerGameplayCamera.GetComponent<HDAdditionalCameraData>();

        hdMainCamera.xrRendering = false;
        playerGameplayCamera.stereoTargetEye = StereoTargetEyeMask.None;
        playerGameplayCamera.depth = menuCamera.depth - 1;
        playerGameplayCamera.enabled = false;

        hdUICamera.xrRendering = true;
        menuCamera.stereoTargetEye = StereoTargetEyeMask.Both;
        menuCamera.nearClipPlane = 0.01f;
        menuCamera.farClipPlane = 15f;
        menuCamera.enabled = true;

        FindObjectsOfType<CanvasTransformFollow>().Do(follow => follow.ResetPosition(true));

        if (!Plugin.Config.CameraResolutionGlobal.Value)
            XRSettings.eyeTextureResolutionScale = 1;
    }

    private void SwitchToGameCamera()
    {
        var hdUICamera = menuCamera.GetComponent<HDAdditionalCameraData>();
        var hdMainCamera = playerGameplayCamera.GetComponent<HDAdditionalCameraData>();

        hdUICamera.xrRendering = false;
        menuCamera.stereoTargetEye = StereoTargetEyeMask.None;
        menuCamera.enabled = false;

        hdMainCamera.xrRendering = true;
        playerGameplayCamera.stereoTargetEye = StereoTargetEyeMask.Both;
        playerGameplayCamera.depth = menuCamera.depth + 1;
        playerGameplayCamera.enabled = true;

        if (!Plugin.Config.CameraResolutionGlobal.Value)
            XRSettings.eyeTextureResolutionScale = Plugin.Config.CameraResolution.Value;
    }

    public static void VibrateController(XRNode hand, float duration, float amplitude)
    {
        UnityEngine.XR.InputDevice device = InputDevices.GetDeviceAtXRNode(hand);

        if (device != null && device.TryGetHapticCapabilities(out HapticCapabilities capabilities) && capabilities.supportsImpulse)
        {
            device.SendHapticImpulse(0, amplitude, duration);
        }
    }
    #endregion
}
