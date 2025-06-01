using HarmonyLib;
using LCVR.Assets;
using LCVR.Patches;
using LCVR.Player;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LCVR.Physics.Interactions.Door;

public class ShowerDoor : MonoBehaviour, VRInteractable
{
    private InteractTrigger trigger;
    
    private void Awake()
    {
        trigger = GetComponentInParent<InteractTrigger>();
    }

    public InteractableFlags Flags => InteractableFlags.BothHands;
    
    public bool OnButtonPress(VRInteractor interactor)
    {
        trigger.Interact(VRSession.Instance.LocalPlayer.transform);

        return true;
    }
    
    public void OnColliderEnter(VRInteractor _) {}
    public void OnColliderExit(VRInteractor _) {}
    public void OnButtonRelease(VRInteractor _) {}
}

[LCVRPatch]
[HarmonyPatch]
internal static class ShowerDoorPatches
{
    [HarmonyPatch(typeof(InteractTrigger), nameof(InteractTrigger.Start))]
    [HarmonyPostfix]
    private static void OnInteractStart(InteractTrigger __instance)
    {
        if (Plugin.Config.DisableDoorInteraction.Value)
            return;
        
        if (__instance.GetComponentInParent<NetworkObject>() is not { } networkObject)
            return;

        if (networkObject.name != "BathroomShowerDoor(Clone)")
            return;

        // Make sure default ray based interaction no longer works for this door
        __instance.gameObject.name = "DoorInteractable";
        
        var interactableObject = Object.Instantiate(AssetManager.Interactable, __instance.transform);
        interactableObject.transform.localPosition = new Vector3(-0.408f, 0.05f, -0.0483f);
        interactableObject.transform.localEulerAngles = Vector3.zero;
        interactableObject.transform.localScale = new Vector3(0.05f, 0.7f, 0.1f);
        interactableObject.AddComponent<ShowerDoor>();
    }
}