using HarmonyLib;
using LCVR.Assets;
using LCVR.Managers;
using LCVR.Patches;
using LCVR.Player;
using UnityEngine;

namespace LCVR.Physics.Interactions;

internal class CompanyBell : MonoBehaviour, VRInteractable
{
    private InteractTrigger trigger;
    private float lastInteractTime;

    private bool CanInteract => Time.realtimeSinceStartup - lastInteractTime > 0.075f;

    public InteractableFlags Flags => InteractableFlags.BothHands;

    private void Awake()
    {
        trigger = GetComponentInParent<InteractTrigger>();
    }

    public void OnColliderEnter(VRInteractor _)
    {
        if (!trigger.interactable || !CanInteract)
            return;

        lastInteractTime = Time.realtimeSinceStartup;
        trigger.Interact(VRSession.Instance.LocalPlayer.transform);
    }

    public bool OnButtonPress(VRInteractor _) { return false; }
    public void OnButtonRelease(VRInteractor _) { }
    public void OnColliderExit(VRInteractor _) { }
}

[LCVRPatch]
[HarmonyPatch]
internal static class DepositItemsDeskPatches
{
    [HarmonyPatch(typeof(DepositItemsDesk), nameof(DepositItemsDesk.Start))]
    [HarmonyPostfix]
    private static void OnItemDeskActivate()
    {
        if (Plugin.Config.DisableCompanyBellInteraction.Value)
            return;

        var bellObject = GameObject.Find("BellDinger/Trigger");
        bellObject.name = "CompanyBellTrigger";

        var bellInteractableObject = Object.Instantiate(AssetManager.Interactable, bellObject.transform);

        bellInteractableObject.transform.localPosition = new Vector3(-0.135f, -0.06f, 0);
        bellInteractableObject.transform.localScale = Vector3.one * 0.5f;

        bellInteractableObject.AddComponent<CompanyBell>();
    }
}
