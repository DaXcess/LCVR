﻿using LCVR.Assets;
using LCVR.Player;
using UnityEngine;

namespace LCVR.Physics.Interactions;

internal class MonitorButton : MonoBehaviour, VRInteractable
{
    private InteractTrigger trigger;
    private float lastInteractTime;

    private AudioSource audioSource;
    private AudioClip buttonPressSFX;

    public MonitorButton otherButton;

    public bool CanInteract => Time.realtimeSinceStartup - lastInteractTime > 0.25f;

    public InteractableFlags Flags => InteractableFlags.BothHands;

    void Awake()
    {
        trigger = GetComponentInParent<InteractTrigger>();
        trigger.gameObject.name = "MonitorButtonInteractable";

        audioSource = gameObject.AddComponent<AudioSource>();
        buttonPressSFX = ShipBuildModeManager.Instance.beginPlacementSFX;
    }

    public void OnColliderEnter(VRInteractor _)
    {
        if (!CanInteract || !otherButton.CanInteract)
            return;

        lastInteractTime = Time.realtimeSinceStartup;
        trigger.onInteract?.Invoke(VRSession.Instance.LocalPlayer.PlayerController);
        audioSource.PlayOneShot(buttonPressSFX);
    }

    public void OnColliderExit(VRInteractor _) { }
    public bool OnButtonPress(VRInteractor _) { return false; }
    public void OnButtonRelease(VRInteractor _) { }

    public static void Create()
    {
        if (Plugin.Config.DisableMonitorInteraction.Value)
            return;

        var cameraOnButtonObject = GameObject.Find("CameraMonitorOnButton");
        var cameraSwitchButtonObject = GameObject.Find("CameraMonitorSwitchButton");

        cameraOnButtonObject.transform.localPosition = new Vector3(-1.3889f, -1.3776f, -1.1147f);
        cameraOnButtonObject.transform.localEulerAngles = new Vector3(0, 82, 270);

        cameraSwitchButtonObject.transform.localPosition = new Vector3(-1.3456f, -1.1547f, -1.1147f);
        cameraSwitchButtonObject.transform.localEulerAngles = new Vector3(0, 82, 270);

        var onOffInteractableObject = Instantiate(AssetManager.interactable, cameraOnButtonObject.transform.GetChild(0));
        var switchInteractableObject = Instantiate(AssetManager.interactable, cameraSwitchButtonObject.transform.GetChild(0));

        onOffInteractableObject.transform.localEulerAngles = switchInteractableObject.transform.localEulerAngles = new Vector3(0, 10, 0);
        onOffInteractableObject.transform.localScale = switchInteractableObject.transform.localScale = new Vector3(0.5f, 1, 0.5f);

        var onOffInteractable = onOffInteractableObject.AddComponent<MonitorButton>();
        var switchInteractable = switchInteractableObject.AddComponent<MonitorButton>();

        onOffInteractable.otherButton = switchInteractable;
        switchInteractable.otherButton = onOffInteractable;
    }
}
