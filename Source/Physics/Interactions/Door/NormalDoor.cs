using HarmonyLib;
using LCVR.Assets;
using LCVR.Managers;
using LCVR.Patches;
using LCVR.Player;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LCVR.Physics.Interactions.Door;

/// <summary>
/// "NormalDoor" isn't really a normal generic door (it's special, but called Normal, I love gamedev!!!)
/// </summary>
public class NormalDoor : MonoBehaviour, VRInteractable
{
    private InteractTrigger trigger;
    private AnimatedObjectTrigger animatedTrigger;

    private bool wasOpened;

    public InteractableFlags Flags => InteractableFlags.BothHands;
    
    private void Awake()
    {
        trigger = GetComponentInParent<InteractTrigger>();
        animatedTrigger = GetComponentInParent<AnimatedObjectTrigger>();

        wasOpened = animatedTrigger.boolValue;
    }

    private void Update()
    {
        switch (animatedTrigger.boolValue)
        {
            case false when wasOpened:
                transform.localScale *= 0.25f;
                break;
            case true when !wasOpened:
                transform.localScale *= 4f;
                break;
        }

        wasOpened = animatedTrigger.boolValue;
    }

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
internal static class NormalDoorPatches
{
    [HarmonyPatch(typeof(InteractTrigger), nameof(InteractTrigger.Start))]
    [HarmonyPostfix]
    private static void OnInteractStart(InteractTrigger __instance)
    {
        if (Plugin.Config.DisableDoorInteraction.Value)
            return;
        
        if (__instance.GetComponentInParent<NetworkObject>() is not { } networkObject)
            return;

        if (networkObject.name != "NormalDoor(Clone)")
            return;

        // Make sure default ray based interaction no longer works for this door
        __instance.gameObject.name = "DoorInteractable";

        var trigger = __instance.GetComponent<AnimatedObjectTrigger>();
        var interactableObject = Object.Instantiate(AssetManager.Interactable, __instance.transform);
        
        interactableObject.transform.localPosition = new Vector3(-0.286f, 0.234f, -0.027f);
        interactableObject.transform.localEulerAngles = Vector3.zero;
        interactableObject.transform.localScale = new Vector3(0.93f, 0.17f, 0.02f) * (trigger.boolValue ? 4 : 1);
        interactableObject.AddComponent<NormalDoor>();
    }
}