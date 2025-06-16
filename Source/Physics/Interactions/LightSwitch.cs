using HarmonyLib;
using LCVR.Assets;
using LCVR.Managers;
using LCVR.Patches;
using LCVR.Player;
using UnityEngine;

namespace LCVR.Physics.Interactions;

public class LightSwitch : MonoBehaviour, VRInteractable
{
    private InteractTrigger trigger;
    private float lastInteraction;

    public InteractableFlags Flags => InteractableFlags.BothHands;
    
    private void Awake()
    {
        trigger = GetComponentInParent<InteractTrigger>();
    }
    
    public void OnColliderEnter(VRInteractor interactor)
    {
        if (!interactor.FingerCurler.IsPointer || !trigger.interactable || Time.realtimeSinceStartup - lastInteraction < 0.25f)
            return;

        lastInteraction = Time.realtimeSinceStartup;
        trigger.Interact(VRSession.Instance.LocalPlayer.transform);
    }

    public void OnColliderExit(VRInteractor interactor)
    {
    }

    public bool OnButtonPress(VRInteractor interactor)
    {
        return false;
    }

    public void OnButtonRelease(VRInteractor interactor)
    {
    }
}

[LCVRPatch]
[HarmonyPatch]
internal static class LightSwitchPatches
{
    [HarmonyPatch(typeof(InteractTrigger), nameof(InteractTrigger.Start))]
    [HarmonyPostfix]
    private static void OnInteractTriggerStart(InteractTrigger __instance)
    {
        if (Plugin.Config.DisableLightSwitchInteraction.Value)
            return;
        
        var go = __instance.gameObject;
        if (go.name is not "LightSwitch")
            return;

        var interactableObject = Object.Instantiate(AssetManager.Interactable, __instance.transform);

        interactableObject.transform.localPosition = new Vector3(0, -0.04f, 0.02f);
        interactableObject.transform.localScale = new Vector3(0.05f, 0.07f, 0.11f);
        
        interactableObject.AddComponent<LightSwitch>();
    }
}