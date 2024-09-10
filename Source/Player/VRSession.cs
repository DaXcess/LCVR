using HarmonyLib;
using LCVR.Assets;
using LCVR.Input;
using LCVR.Physics.Interactions;
using LCVR.UI;
using Microsoft.MixedReality.Toolkit.Experimental.UI;
using System.Collections.Generic;
using System.Linq;
using LCVR.Physics.Interactions.Car;
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
    /// Whether the game has VR enabled. This field will only be populated after LCVR has loaded.
    /// </summary>
    public static bool InVR => Plugin.Flags.HasFlag(Flags.VR);

    #region Custom Camera

    private bool customCameraEnabled;
    private Camera customCamera;
    private float customCameraLerpFactor;

    #endregion

    #region Public Accessors

    public VRPlayer LocalPlayer { get; private set; }
    public VRHUD HUD { get; private set; }

    public Camera MainCamera { get; private set; }
    public Camera UICamera { get; private set; }

    public Rendering.VolumeManager VolumeManager { get; private set; }

    public InteractionManager InteractionManager { get; private set; }
    public CarManager CarManager { get; private set; }
    public MuffleManager MuffleManager { get; private set; }
    public Muffler LocalMuffler { get; private set; }
    public Face Face { get; private set; }

    #endregion

    private void Awake()
    {
        Instance = this;

        MainCamera = StartOfRound.Instance.activeCamera;
        UICamera = GameObject.Find("UICamera").GetComponent<Camera>();

        if (InVR)
            InitializeVRSession();

        // Initialize universal interactions
        ChargeStation.Create();
        ShipLever.Create();
        
        MuffleManager = new MuffleManager();
        CarManager = new CarManager();

        if (Plugin.Flags.HasFlag(Flags.InteractableDebug))
            MainCamera.cullingMask |= 1 << 11;
    }

    private void LateUpdate()
    {
        if (!InVR)
            return;

        if (customCameraEnabled)
        {
            customCamera.transform.position = MainCamera.transform.position;
            customCamera.transform.rotation = Quaternion.Lerp(customCamera.transform.rotation,
                MainCamera.transform.rotation, customCameraLerpFactor);
        }
    }

    private void InitializeVRSession()
    {
        // Disable base UI input system
        var input = GameObject.Find("EventSystem")?.GetComponent<InputSystemUIInputModule>();
        if (input != null)
            input.enabled = false;

        // Set up helmet model

        // Move around the volumetric plane
        var helmetContainer = GameObject.Find("Systems/Rendering/PlayerHUDHelmetModel");
        var helmetModel = helmetContainer.Find("ScavengerHelmet");
        helmetModel.transform.Find("Plane").SetParent(helmetContainer.transform);

        // Toggle helmet visor visibility
        helmetModel.SetActive(Plugin.Config.EnableHelmetVisor.Value);
        
        // Listen to setting change for helmet model
        Plugin.Config.EnableHelmetVisor.SettingChanged +=
            (_, _) => helmetModel.SetActive(Plugin.Config.EnableHelmetVisor.Value);

        // Move helmet model to child of target point
        var helmetTarget = StartOfRound.Instance.localPlayerController.gameObject
            .Find("ScavengerModel/metarig/CameraContainer/MainCamera/HUDHelmetPosition").transform;
        helmetContainer.transform.SetParent(helmetTarget, false);

        helmetContainer.transform.localPosition = Vector3.zero;
        helmetContainer.transform.localEulerAngles = Vector3.zero;

        // Disable shadows on the helmet models
        helmetContainer.GetComponentsInChildren<MeshRenderer>(true)
            .Do(renderer => renderer.shadowCastingMode = ShadowCastingMode.Off);

        helmetTarget.localPosition = new Vector3(0.01f, -0.068f, -0.073f);
        helmetTarget.localScale = Vector3.one;

        // Disable UI camera and promote FPV camera
        MainCamera.targetTexture = null;
        UICamera.GetComponent<HDAdditionalCameraData>().xrRendering = false;
        UICamera.stereoTargetEye = StereoTargetEyeMask.None;
        UICamera.enabled = false;

        MainCamera.stereoTargetEye = StereoTargetEyeMask.Both;
        MainCamera.GetComponent<HDAdditionalCameraData>().xrRendering = true;

        MainCamera.depth = UICamera.depth + 1;

        // Create HMD tracker
        var cameraPoseDriver = MainCamera.gameObject.AddComponent<TrackedPoseDriver>();
        cameraPoseDriver.positionAction = Actions.Instance.HeadPosition;
        cameraPoseDriver.rotationAction = Actions.Instance.HeadRotation;
        cameraPoseDriver.trackingStateInput = new InputActionProperty(Actions.Instance.HeadTrackingState);

        // Setup Pause Menu Camera
        var uiCameraAnchor = new GameObject("UI Camera Anchor")
        {
            transform =
            {
                position = new Vector3(0, -1000, 0)
            }
        };

        UICamera.transform.SetParent(uiCameraAnchor.transform, false);
        UICamera.cullingMask = -1;

        uiCameraAnchor.AddComponent<VRPauseMenu>();

        var uiCameraPoseDriver = UICamera.gameObject.AddComponent<TrackedPoseDriver>();
        uiCameraPoseDriver.positionAction = Actions.Instance.HeadPosition;
        uiCameraPoseDriver.rotationAction = Actions.Instance.HeadRotation;
        uiCameraPoseDriver.trackingStateInput = new InputActionProperty(Actions.Instance.HeadTrackingState);

        // Apply optimization configuration
        var hdCamera = MainCamera.GetComponent<HDAdditionalCameraData>();

        hdCamera.allowDynamicResolution = Plugin.Config.EnableDynamicResolution.Value;

        hdCamera.DisableQualitySetting(FrameSettingsField.DepthOfField);
        hdCamera.DisableQualitySetting(FrameSettingsField.SSAO);
        hdCamera.DisableQualitySetting(FrameSettingsField.SSAOAsync);

        if (Plugin.Config.DisableVolumetrics.Value)
            hdCamera.DisableQualitySetting(FrameSettingsField.Volumetrics);

        // Handle volumetric setting change
        Plugin.Config.DisableVolumetrics.SettingChanged += (_, _) =>
        {
            if (Plugin.Config.DisableVolumetrics.Value)
                hdCamera.DisableQualitySetting(FrameSettingsField.Volumetrics);
            else
                hdCamera.EnableQualitySetting(FrameSettingsField.Volumetrics);
        };
        
        XRSettings.eyeTextureResolutionScale = Plugin.Config.CameraResolution.Value;

        // Disable lens distortion effects
        var profiles = new[]
        {
            HUDManager.Instance.insanityScreenFilter.profile,
        };

        var extendedProfiles = new[]
        {
            HUDManager.Instance.drunknessFilter.profile,
            HUDManager.Instance.flashbangScreenFilter.profile,
            HUDManager.Instance.underwaterScreenFilter.profile,
        };

        var distortionFilters = new List<LensDistortion>();

        distortionFilters.AddRange(profiles.SelectMany(profile =>
            profile.components.FindAll(component => component is LensDistortion)
                .Select(component => component as LensDistortion)));

        if (Plugin.Config.DisableLensDistortion.Value)
            distortionFilters.AddRange(extendedProfiles.SelectMany(profile =>
                profile.components.FindAll(component => component is LensDistortion)
                    .Select(component => component as LensDistortion)));

        distortionFilters.ForEach(filter => filter.active = false);

        // Initialize secondary custom camera
        if (Plugin.Config.EnableCustomCamera.Value)
            InitializeCustomCamera();

        // Add keyboard to Terminal
        var terminal = FindObjectOfType<Terminal>();

        var terminalKeyboardObject = Instantiate(AssetManager.Keyboard, terminal.transform.parent.parent);
        terminalKeyboardObject.transform.localPosition = new Vector3(-0.584f, 0.333f, 0.791f);
        terminalKeyboardObject.transform.localEulerAngles = new Vector3(0, 90, 90);
        terminalKeyboardObject.transform.localScale = Vector3.one * 0.0009f;

        terminalKeyboardObject.GetComponent<Canvas>().worldCamera = MainCamera;

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

        terminalKeyboard.OnTextSubmitted += (_, _) => { terminal.OnSubmit(); };

        terminalKeyboard.OnMacroTriggered += (text) =>
        {
            terminal.screenText.text =
                terminal.screenText.text.Substring(0, terminal.screenText.text.Length - terminal.textAdded);
            terminal.screenText.text += text;
            terminal.textAdded = text.Length;
            terminal.OnSubmit();
        };

        terminalKeyboard.OnClosed += (_, _) => { terminal.QuitTerminal(); };

        // Initialize VR Player
        LocalPlayer = StartOfRound.Instance.localPlayerController.gameObject.AddComponent<VRPlayer>();

        // Initialize Interaction Manager
        InteractionManager = new InteractionManager();

        // Initialize HUD
        HUD = new GameObject("VR HUD").AddComponent<VRHUD>();
        HUD.TerminalKeyboard = terminalKeyboard;

        // Initialize VR-Only interactions
        
        // Creates interactors for both monitor buttons, and also moves the buttons into a more accessible position
        MonitorButton.Create();

        // Creates interactors for both ship door buttons
        ShipDoorButton.Create();

        // Creates interactor for muting yourself using your hand
        LocalMuffler = Muffler.Create();

        // Creates interactor for using items when held up to your face
        Face = Face.Create();

        // Teleporters, the ship horn and doors are not guaranteed to be present when spawning in a lobby,
        // so they are initialized with the help of patches on the "Start" and "Awake" lifetime methods

        // Apply interactions config

        VRController.ResetDisabledInteractTriggers();

        // Ship Lever
        if (!Plugin.Config.DisableShipLeverInteraction.Value)
            VRController.DisableInteractTrigger("StartGameLever");

        // Charging Station
        if (!Plugin.Config.DisableChargeStationInteraction.Value)
            VRController.DisableInteractTrigger("ChargingStationTrigger");

        // Teleporter
        if (!Plugin.Config.DisableTeleporterInteraction.Value)
        {
            VRController.DisableInteractTrigger("TeleporterButtonGlass");
            VRController.DisableInteractTrigger("TeleporterRedButton");
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

        // Hangar Lever
        if (!Plugin.Config.DisableHangarLeverInteraction.Value)
            VRController.DisableInteractTrigger("LeverSwitchInteractable");

        if (!Plugin.Config.DisableElevatorButtonInteraction.Value)
            VRController.DisableInteractTrigger("ElevatorButtonTrigger");
        
        // Car horn
        if (!Plugin.Config.DisableCarHonkInteraction.Value)
            VRController.DisableInteractTrigger("HonkHornInteractable");

        // Car eject button
        if (!Plugin.Config.DisableCarEjectInteraction.Value)
        {
            VRController.DisableInteractTrigger("EjectButtonGlass");
            VRController.DisableInteractTrigger("EjectRedButton");
        }
        
        // Generic car button
        if (!Plugin.Config.DisableCarButtonInteractions.Value)
            VRController.DisableInteractTrigger("CarButton");
        
        // Car ignition
        if (!Plugin.Config.DisableCarIgnitionInteractions.Value)
        {
            VRController.DisableInteractTrigger("StartIgnition");
            VRController.DisableInteractTrigger("StopIgnition");
        }
        
        // Car gear shift
        if (!Plugin.Config.DisableCarGearStickInteractions.Value)
        {
            VRController.DisableInteractTrigger("ShiftToReverseTrigger");
            VRController.DisableInteractTrigger("ShiftToDriveTrigger");
            VRController.DisableInteractTrigger("ShiftToParkTrigger");
        }
        
#if DEBUG
        Experiments.Experiments.RunExperiments();
#endif

        // Misc
        VolumeManager = Instantiate(AssetManager.VolumeManager, transform).GetComponent<Rendering.VolumeManager>();
    }

    #region VR

    private void InitializeCustomCamera()
    {
        customCameraEnabled = true;
        customCameraLerpFactor = Mathf.Clamp(Plugin.Config.CustomCameraLerpFactor.Value, 0.01f, 1f);

        var children = MainCamera.transform.GetChildren();

        children.Do(child => child.SetParent(null, true));

        customCamera = Instantiate(MainCamera, transform);
        customCamera.name = "Custom Camera";
        customCamera.transform.localEulerAngles = Vector3.zero;
        customCamera.transform.localPosition = Vector3.zero;
        customCamera.transform.localScale = Vector3.one;

        customCamera.fieldOfView = Plugin.Config.CustomCameraFOV.Value;
        customCamera.depth++;
        customCamera.stereoTargetEye = StereoTargetEyeMask.None;
        customCamera.targetDisplay = 0;

        // Prevent cloned camera from tracking HMD movement
        Destroy(customCamera.GetComponent<TrackedPoseDriver>());

        var hdDesktopCamera = customCamera.GetComponent<HDAdditionalCameraData>();
        hdDesktopCamera.xrRendering = false;

        children.Do(child => child.SetParent(MainCamera.transform, true));

        // Settings listeners
        Plugin.Config.CustomCameraFOV.SettingChanged +=
            (_, _) => customCamera.fieldOfView = Plugin.Config.CustomCameraFOV.Value;
        Plugin.Config.CustomCameraLerpFactor.SettingChanged += (_, _) =>
            customCameraLerpFactor = Mathf.Clamp(Plugin.Config.CustomCameraLerpFactor.Value, 0.01f, 1f);
    }

    public void OnEnterTerminal()
    {
        HUD.TerminalKeyboard.PresentKeyboard();

        LocalPlayer.EnableInteractorVisuals();
        LocalPlayer.PrimaryController.ShowDebugInteractorVisual(false);
    }

    public void OnExitTerminal()
    {
        if (HUD.TerminalKeyboard.isActiveAndEnabled)
            HUD.TerminalKeyboard.Close();

        LocalPlayer.EnableInteractorVisuals(false);
        LocalPlayer.PrimaryController.ShowDebugInteractorVisual();
    }

    public void OnPauseMenuOpened()
    {
        // Make sure keyboard is closed when pause menu opens
        HUD.MenuKeyboard.Close();
        SwitchToUICamera();

        if (customCameraEnabled)
            customCamera.enabled = false;

        LocalPlayer.PrimaryController.enabled = false;
        LocalPlayer.LeftHandInteractor.enabled = false;
        LocalPlayer.RightHandInteractor.enabled = false;
    }

    public void OnPauseMenuClosed()
    {
        HUD.MenuKeyboard.Close();
        SwitchToGameCamera();

        if (customCameraEnabled)
            customCamera.enabled = true;

        LocalPlayer.PrimaryController.enabled = true;
        LocalPlayer.LeftHandInteractor.enabled = !LocalPlayer.PlayerController.isPlayerDead;
        LocalPlayer.RightHandInteractor.enabled = !LocalPlayer.PlayerController.isPlayerDead;
    }

    private void SwitchToUICamera()
    {
        var hdUICamera = UICamera.GetComponent<HDAdditionalCameraData>();
        var hdMainCamera = MainCamera.GetComponent<HDAdditionalCameraData>();

        hdMainCamera.xrRendering = false;
        MainCamera.stereoTargetEye = StereoTargetEyeMask.None;
        MainCamera.depth = UICamera.depth - 1;
        MainCamera.enabled = false;

        hdUICamera.xrRendering = true;
        UICamera.stereoTargetEye = StereoTargetEyeMask.Both;
        UICamera.nearClipPlane = 0.01f;
        UICamera.farClipPlane = 150f;
        UICamera.enabled = true;
        
        XRSettings.eyeTextureResolutionScale = 1.2f;

        FindObjectsOfType<CanvasTransformFollow>().Do(follow => follow.ResetPosition(true));
    }

    private void SwitchToGameCamera()
    {
        var hdUICamera = UICamera.GetComponent<HDAdditionalCameraData>();
        var hdMainCamera = MainCamera.GetComponent<HDAdditionalCameraData>();

        hdUICamera.xrRendering = false;
        UICamera.stereoTargetEye = StereoTargetEyeMask.None;
        UICamera.enabled = false;

        hdMainCamera.xrRendering = true;
        MainCamera.stereoTargetEye = StereoTargetEyeMask.Both;
        MainCamera.depth = UICamera.depth + 1;
        MainCamera.enabled = true;

        XRSettings.eyeTextureResolutionScale = Plugin.Config.CameraResolution.Value;
    }

    public static void VibrateController(XRNode hand, float duration, float amplitude)
    {
        var device = InputDevices.GetDeviceAtXRNode(hand);

        if (device.isValid && device.TryGetHapticCapabilities(out var capabilities) &&
            capabilities.supportsImpulse)
        {
            device.SendHapticImpulse(0, amplitude, duration);
        }
    }

    #endregion
}
