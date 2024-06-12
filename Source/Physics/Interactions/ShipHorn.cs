using HarmonyLib;
using LCVR.Assets;
using LCVR.Patches;
using LCVR.Player;
using UnityEngine;

namespace LCVR.Physics.Interactions;

internal class ShipHorn : MonoBehaviour, VRInteractable
{
    private ShipAlarmCord shipHorn;
    private InteractTrigger trigger;
    private Animator animator;
    private Transform pullCord;

    private Transform targetTransform;
    private float grabYPosition;

    public InteractableFlags Flags => InteractableFlags.BothHands;

    private void Awake()
    {
        shipHorn = GetComponentInParent<ShipAlarmCord>();
        trigger = GetComponentInParent<InteractTrigger>();
        animator = GetComponentInParent<Animator>();
        pullCord = transform.parent.parent;
    }

    private void Update()
    {
        if (targetTransform is null)
            return;

        var heightOffset = Mathf.Clamp(targetTransform.position.y - grabYPosition, -0.105f, 0);
        pullCord.localPosition = new Vector3(pullCord.transform.localPosition.x, 2.994f + heightOffset, pullCord.localPosition.z);

        if (heightOffset < -0.08f)
            shipHorn.HoldCordDown();
    }

    public bool OnButtonPress(VRInteractor interactor)
    {
        if (shipHorn.otherClientHoldingCord || !trigger.interactable)
            return false;

        targetTransform = interactor.transform;
        animator.enabled = false;
        grabYPosition = targetTransform.position.y;
        
        interactor.FingerCurler.ForceFist(true);

        return true;
    }

    public void OnButtonRelease(VRInteractor interactor)
    {
        if (targetTransform is not null)
        {
            shipHorn.StopHorn();
            targetTransform = null;
            animator.enabled = true;
        }
        
        interactor.FingerCurler.ForceFist(false);
    }

    public void OnColliderEnter(VRInteractor _) { }
    public void OnColliderExit(VRInteractor _) { }
}

[LCVRPatch]
[HarmonyPatch]
internal static class ShipAlarmCordPatches
{
    [HarmonyPatch(typeof(ShipAlarmCord), nameof(ShipAlarmCord.Start))]
    [HarmonyPostfix]
    private static void OnShipHornInitialized(ShipAlarmCord __instance)
    {
        if (Plugin.Config.DisableShipHornInteraction.Value)
            return;

        __instance.gameObject.name = "ShipHornPullInteractable";

        var interactableObject = Object.Instantiate(AssetManager.Interactable, __instance.transform);

        interactableObject.transform.localPosition = new Vector3(0, 0, -0.28f);
        interactableObject.transform.localScale = Vector3.one * 0.33f;
        interactableObject.AddComponent<ShipHorn>();
    }
}
