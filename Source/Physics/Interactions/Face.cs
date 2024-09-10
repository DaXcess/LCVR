using GameNetcodeStuff;
using HarmonyLib;
using LCVR.Assets;
using LCVR.Patches;
using LCVR.Player;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LCVR.Physics.Interactions;

public class Face : MonoBehaviour, VRInteractable
{
    private readonly List<string> ALLOWED_ITEMS = [
        "Walkie-talkie",
        "TZP-Inhalant",
        "Comedy",
        "Tragedy",
    ];

    private GrabbableObject heldItem;
    private bool isInteracting;

    private Coroutine stopInteractingCoroutine;

    public InteractableFlags Flags => InteractableFlags.RightHand;
    public bool IsInteracting => isInteracting;

    private bool CanInteract => VRSession.Instance.LocalPlayer.PlayerController.CanUseItem();

    private void Update()
    {
        if (!isInteracting)
            return;

        var item = GetItem();
        if (!item || heldItem == item ||
            VRSession.Instance.LocalPlayer.PlayerController.timeSinceSwitchingSlots < 0.075f)
            return;

        heldItem = item;
        heldItem.UseItemOnClient();
    }

    public void OnColliderEnter(VRInteractor _)
    {
        if (stopInteractingCoroutine != null)
        {
            StopCoroutine(stopInteractingCoroutine);
            stopInteractingCoroutine = null;

            return;
        }

        var item = GetItem();

        if (item == null || !CanInteract)
            return;

        isInteracting = true;
        heldItem = item;

        heldItem.UseItemOnClient();
    }

    public void OnColliderExit(VRInteractor _)
    {
        stopInteractingCoroutine = StartCoroutine(DelayedStopUsing());
    }

    private IEnumerator DelayedStopUsing()
    {
        yield return new WaitForSeconds(0.25f);

        isInteracting = false;
        heldItem = null;

        var item = GetItem();

        if (CanInteract)
            item?.UseItemOnClient(false);

        stopInteractingCoroutine = null;
    }

    public bool OnButtonPress(VRInteractor _) { return false; }
    public void OnButtonRelease(VRInteractor _) { }

    private GrabbableObject GetItem()
    {
        var item = VRSession.Instance.LocalPlayer.PlayerController.currentlyHeldObjectServer;

        if (!item || !item.itemProperties || !ALLOWED_ITEMS.Contains(item.itemProperties.itemName))
            return null;

        return item;
    }

    public static Face Create()
    {
        if (Plugin.Config.DisableFaceInteractions.Value)
            return null;

        var interactableObject = Instantiate(AssetManager.Interactable, VRSession.Instance.MainCamera.transform);
        interactableObject.transform.localPosition = new Vector3(0, -0.1f, 0.1f);
        interactableObject.transform.localScale = new Vector3(0.225f, 0.2f, 0.225f);

        return interactableObject.AddComponent<Face>();
    }
}

[LCVRPatch]
[HarmonyPatch]
internal static class FaceItemInteractionPatches
{
    /// <summary>
    /// Prevent the item activation keybind from doing anything if we're currently holding an active item up to our face
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.ActivateItem_performed))]
    [HarmonyPrefix]
    private static bool CanInteractUsingController()
    {
        return VRSession.Instance?.Face is not { IsInteracting: true };
    }

    /// <summary>
    /// Prevent the item activation keybind from doing anything if we're currently holding an active item up to our face
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.ActivateItem_canceled))]
    [HarmonyPrefix]
    private static bool CanStopInteractUsingController()
    {
        return VRSession.Instance?.Face is not { IsInteracting: true };
    }
}
