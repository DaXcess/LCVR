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

    private bool BlastingLocally => (bool)AccessTools.Field(typeof(ShipAlarmCord), "localClientHoldingCord").GetValue(shipHorn);
    private bool BlastingByOther => (bool)AccessTools.Field(typeof(ShipAlarmCord), "otherClientHoldingCord").GetValue(shipHorn);

    public InteractableFlags Flags => InteractableFlags.BothHands;

    void Awake()
    {
        shipHorn = GetComponentInParent<ShipAlarmCord>();
        trigger = GetComponentInParent<InteractTrigger>();
        animator = GetComponentInParent<Animator>();
        pullCord = transform.parent.parent;
    }

    void Update()
    {
        if (targetTransform == null)
            return;

        var heightOffset = Mathf.Clamp(targetTransform.position.y - grabYPosition, -0.105f, 0);
        pullCord.transform.localPosition = new Vector3(pullCord.transform.localPosition.x, 2.994f + heightOffset, pullCord.transform.localPosition.z);

        if (heightOffset < -0.08f)
            shipHorn.HoldCordDown();
    }

    public bool OnButtonPress(VRInteractor interactor)
    {
        if (BlastingByOther || !trigger.interactable)
            return false;

        targetTransform = interactor.transform;
        animator.enabled = false;
        grabYPosition = targetTransform.position.y;

        return true;
    }

    public void OnButtonRelease(VRInteractor _)
    {
        if (targetTransform != null)
        {
            shipHorn.StopHorn();
            targetTransform = null;
            animator.enabled = true;
        }
    }

    public void OnColliderEnter(VRInteractor _) { }
    public void OnColliderExit(VRInteractor _) { }
}

[LCVRPatch]
[HarmonyPatch]
internal static class ShipAlarmCordPatches
{
    [HarmonyPatch(typeof(ShipAlarmCord), "Start")]
    [HarmonyPostfix]
    private static void OnShipHornInitialized(ShipAlarmCord __instance)
    {
        if (Plugin.Config.DisableShipHornInteraction.Value)
            return;

        __instance.gameObject.name = "ShipHornPullInteractable";

        var interactableObject = Object.Instantiate(AssetManager.interactable, __instance.transform);

        interactableObject.transform.localPosition = new Vector3(0, 0, -0.28f);
        interactableObject.transform.localScale = Vector3.one * 0.33f;
        interactableObject.AddComponent<ShipHorn>();
    }
}
