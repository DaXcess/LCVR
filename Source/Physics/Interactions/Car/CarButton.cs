using System.Linq;
using HarmonyLib;
using LCVR.Assets;
using LCVR.Patches;
using LCVR.Player;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LCVR.Physics.Interactions.Car;

public class CarButton : MonoBehaviour, VRInteractable
{
    public InteractableFlags Flags => InteractableFlags.BothHands;

    private InteractTrigger trigger;
    private AudioClip buttonPressSfx;
    private AudioSource audioSource;
    private float lastTriggerTime;

    public CarButton[] otherButtons = [];
    
    private bool CanInteract => trigger.interactable && Time.realtimeSinceStartup - lastTriggerTime > 0.25f;

    private void Awake()
    {
        trigger = GetComponentInParent<InteractTrigger>();
        buttonPressSfx = ShipBuildModeManager.Instance.beginPlacementSFX;
        audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void OnColliderEnter(VRInteractor interactor)
    {
        if (!CanInteract && otherButtons.All(btn => btn.CanInteract))
            return;

        lastTriggerTime = Time.realtimeSinceStartup;
        trigger.onInteract?.Invoke(VRSession.Instance.LocalPlayer.PlayerController);
        audioSource.PlayOneShot(buttonPressSfx);
    }

    public void OnColliderExit(VRInteractor _) { }
    public bool OnButtonPress(VRInteractor _) { return false; }
    public void OnButtonRelease(VRInteractor _) { }
}

[LCVRPatch(LCVRPatchTarget.Universal)]
[HarmonyPatch]
internal static class CarButtonPatches
{
    [HarmonyPatch(typeof(VehicleController), nameof(VehicleController.Awake))]
    [HarmonyPostfix]
    private static void OnCarCreated(VehicleController __instance)
    {
        if (Plugin.Config.DisableCarButtonInteractions.Value)
            return;
        
        var wipers = __instance.transform.Find("Triggers/ChangeChannel (1)");
        var cabinWindow = __instance.transform.Find("Triggers/ChangeChannel (2)");
        var headlights = __instance.transform.Find("Triggers/ChangeChannel (3)");
        var tune = __instance.transform.Find("Triggers/Radio/ChangeChannel");
        var toggleRadio = __instance.transform.Find("Triggers/Radio/TurnOnRadio");
        
        // Make sure VR interact trigger goes away
        wipers.gameObject.name = "CarButton";
        cabinWindow.gameObject.name = "CarButton";
        headlights.gameObject.name = "CarButton";
        tune.gameObject.name = "CarButton";
        toggleRadio.gameObject.name = "CarButton";
        
        var wipersInteract = Object.Instantiate(AssetManager.Interactable, wipers);
        var cabinWindowInteract = Object.Instantiate(AssetManager.Interactable, cabinWindow);
        var headlightsInteract = Object.Instantiate(AssetManager.Interactable, headlights);
        var tuneInteract = Object.Instantiate(AssetManager.Interactable, tune);
        var toggleRadioInteract = Object.Instantiate(AssetManager.Interactable, toggleRadio);

        // Buncha transforms
        wipersInteract.transform.localPosition = new Vector3(-0.1f, 0, 0);
        wipersInteract.transform.localScale = Vector3.one * 0.5f;
        
        cabinWindowInteract.transform.localPosition = new Vector3(-0.1f, 0, 0);
        cabinWindowInteract.transform.localScale = Vector3.one * 0.5f;

        headlightsInteract.transform.localPosition = new Vector3(0.2f, 0, 0);
        headlightsInteract.transform.localScale = Vector3.one * 0.5f;

        tuneInteract.transform.localPosition = new Vector3(0.1f, 0.2f, 0.2f);
        tuneInteract.transform.localScale = Vector3.one * 0.5f;

        toggleRadioInteract.transform.localPosition = new Vector3(0.1f, -0.2f, 0.2f);
        toggleRadioInteract.transform.localScale = Vector3.one * 0.5f;
        
        var wipersButton = wipersInteract.AddComponent<CarButton>();
        var cabinButton = cabinWindowInteract.AddComponent<CarButton>();
        var tuneButton = tuneInteract.AddComponent<CarButton>();
        var toggleButton = toggleRadioInteract.AddComponent<CarButton>();
        headlightsInteract.AddComponent<CarButton>();

        wipersButton.otherButtons = [cabinButton];
        cabinButton.otherButtons = [wipersButton];
        tuneButton.otherButtons = [toggleButton];
        toggleButton.otherButtons = [tuneButton];
    }
}