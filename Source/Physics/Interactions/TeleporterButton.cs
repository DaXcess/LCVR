using HarmonyLib;
using LCVR.Assets;
using LCVR.Patches;
using LCVR.Player;
using System.Collections;
using TMPro;
using UnityEngine;

namespace LCVR.Physics.Interactions;

internal class TeleporterButton : MonoBehaviour, VRInteractable
{
    private ShipTeleporter teleporter;
    private InteractTrigger trigger;
    internal TextMeshProUGUI timerText;
    internal TeleporterButtonGlass glassInteractable;

    public InteractableFlags Flags => InteractableFlags.BothHands;

    void Awake()
    {
        teleporter = GetComponentInParent<ShipTeleporter>();
        trigger = GetComponentInParent<InteractTrigger>();
    }

    void Start()
    {
        StartCoroutine(timerLoop());
    }

    private IEnumerator timerLoop()
    {
        while (true)
        {
            if (teleporter.cooldownTime > 0)
                timerText.color = new Color(1f, 0.1062f, 0f, 0.4314f);
            else
                timerText.color = new Color(0.1062f, 1f, 0f, 0.4314f);

            timerText.text = $"{Mathf.Floor(teleporter.cooldownTime / 60f)}:{(int)teleporter.cooldownTime % 60:D2}";
            yield return new WaitForSeconds(1);
        }
    }

    public void OnColliderEnter(VRInteractor _)
    {
        if (!glassInteractable.CanPressButton || !trigger.interactable)
            return;

        teleporter.PressTeleportButtonOnLocalClient();
    }

    public bool OnButtonPress(VRInteractor _) { return false; }
    public void OnButtonRelease(VRInteractor _) { }
    public void OnColliderExit(VRInteractor _) { }
}

internal class TeleporterButtonGlass : MonoBehaviour, VRInteractable
{
    private AnimatedObjectTrigger trigger;
    private float lastTriggerTime;

    // Check if glass is open and has been opened for at least 200ms to prevent accidental trigger
    public bool CanPressButton => trigger.boolValue && Time.realtimeSinceStartup - lastTriggerTime > 0.2f;
    public InteractableFlags Flags => InteractableFlags.BothHands;

    void Awake()
    {
        trigger = GetComponentInParent<AnimatedObjectTrigger>();
    }

    public void OnColliderEnter(VRInteractor _)
    {
        // Require at least 1s cooldown on glass
        if (Time.realtimeSinceStartup - lastTriggerTime < 1)
            return;

        lastTriggerTime = Time.realtimeSinceStartup;
        trigger.TriggerAnimation(VRSession.Instance.LocalPlayer.PlayerController);
    }

    public bool OnButtonPress(VRInteractor _) { return false; }
    public void OnButtonRelease(VRInteractor _) { }
    public void OnColliderExit(VRInteractor _) { }
}

/// <summary>
/// This patch is used to detect when a teleporter activates so that we're able to attach the interactables
/// </summary>
[LCVRPatch]
[HarmonyPatch]
internal static class TeleporterPatches
{
    [HarmonyPatch(typeof(ShipTeleporter), "Awake")]
    [HarmonyPostfix]
    private static void OnShipTeleporterCreated(ShipTeleporter __instance)
    {
        // HEADS UP!
        // THIS CODE MAY RUN BEFORE VRSession IS INITIALIZED!
        // WE DO **NOT** HAVE ACCESS TO VRSession.Instance!

        if (Plugin.Config.DisableTeleporterInteraction.Value)
            return;

        var button = __instance.buttonTrigger.gameObject;
        var glass = button.transform.parent.Find("ButtonGlass").gameObject;

        var buttonInteractableObject = Object.Instantiate(AssetManager.interactable, button.transform);
        var glassInteractableObject = Object.Instantiate(AssetManager.interactable, glass.transform);

        buttonInteractableObject.transform.localPosition = new Vector3(0, -0.12f, 1.2f);
        buttonInteractableObject.transform.localScale = new Vector3(1.1f, 1.2f, 2);

        glassInteractableObject.transform.localPosition = new Vector3(-1, 0, 1);
        glassInteractableObject.transform.localEulerAngles = new Vector3(0, 315, 0);
        glassInteractableObject.transform.localScale = new Vector3(3, 1.7f, 1.5f);

        var buttonInteractable = buttonInteractableObject.AddComponent<TeleporterButton>();
        var glassInteractable = glassInteractableObject.AddComponent<TeleporterButtonGlass>();

        buttonInteractable.glassInteractable = glassInteractable;

        var timerCanvas = new GameObject("TimerCanvas").AddComponent<Canvas>();
        timerCanvas.transform.parent = glass.transform;
        timerCanvas.transform.localPosition = new Vector3(0, 0, 2.5f);
        timerCanvas.transform.localEulerAngles = new Vector3(0, 140, 90);
        timerCanvas.transform.localScale = Vector3.one * 0.02f;
        timerCanvas.renderMode = RenderMode.WorldSpace;
        timerCanvas.worldCamera = StartOfRound.Instance.activeCamera;

        var timerText = new GameObject("TimerText").AddComponent<TextMeshProUGUI>();
        timerText.transform.parent = timerCanvas.transform;
        timerText.transform.localPosition = new Vector3(3, -75, 0);
        timerText.transform.localEulerAngles = Vector3.zero;
        timerText.transform.localScale = Vector3.one;
        timerText.text = "Inf";
        timerText.alignment = TextAlignmentOptions.Center;
        timerText.font = HUDManager.Instance.signalTranslatorText.font;

        buttonInteractable.timerText = timerText;
    }
}
