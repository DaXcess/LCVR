using HarmonyLib;
using LCVR.Assets;
using LCVR.Patches;
using LCVR.Player;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LCVR.Physics.Interactions.Car;

public class CarHonk : MonoBehaviour, VRInteractable
{
    public InteractableFlags Flags => InteractableFlags.BothHands | InteractableFlags.NotWhileHeld;

    private InteractTrigger trigger;
    
    private void Awake()
    {
        trigger = GetComponentInParent<InteractTrigger>();
    }

    public void OnColliderEnter(VRInteractor interactor)
    {
        if (!trigger.interactable)
            return;

        trigger.HoldInteractNotFilled();
    }

    public void OnColliderExit(VRInteractor interactor)
    {
        trigger.StopInteraction();
    }

    public bool OnButtonPress(VRInteractor interactor)
    {
        return false;
    }

    public void OnButtonRelease(VRInteractor interactor)
    {
    }
}

[LCVRPatch(LCVRPatchTarget.Universal)]
[HarmonyPatch]
internal static class CarHonkPatches
{
    [HarmonyPatch(typeof(VehicleController), nameof(VehicleController.Awake))]
    [HarmonyPostfix]
    private static void OnCarCreated(VehicleController __instance)
    {
        if (Plugin.Config.DisableCarHonkInteraction.Value)
            return;
        
        var honkTrigger = __instance.transform.Find("Triggers/HonkHorn");

        // Make sure VR interact trigger goes away
        honkTrigger.gameObject.name = "HonkHornInteractable";
        
        var honkInteractableObject = Object.Instantiate(AssetManager.Interactable, honkTrigger);
        honkInteractableObject.AddComponent<CarHonk>();
        honkInteractableObject.transform.localScale = Vector3.one * 0.8f;
    }
}