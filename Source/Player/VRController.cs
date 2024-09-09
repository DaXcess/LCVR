using GameNetcodeStuff;
using System;
using UnityEngine.InputSystem;
using UnityEngine;
using UnityEngine.XR;
using LCVR.Input;
using LCVR.Assets;
using LCVR.UI;
using System.Collections.Generic;

namespace LCVR.Player;

public class VRController : MonoBehaviour
{
    private const int INTERACTABLE_OBJECTS_MASK = (1 << 6) | (1 << 8) | (1 << 9) | (1 << 30);

    private static readonly int grabInvalidated = Animator.StringToHash("GrabInvalidated");
    private static readonly int grabValidated = Animator.StringToHash("GrabValidated");
    private static readonly int cancelHolding = Animator.StringToHash("cancelHolding");
    private static readonly int @throw = Animator.StringToHash("Throw");

    private static readonly HashSet<string> DisabledInteractTriggers = [];

    private static InputAction GrabAction => Actions.Instance["Interact"];
    private static PlayerControllerB PlayerController => StartOfRound.Instance.localPlayerController;

    private LineRenderer debugLineRenderer;

    private static string CursorTip
    {
        set
        {
            PlayerController.cursorTip.text =
                value.Replace(": [LMB]", "").Replace(": [RMB]", "").Replace(": [E]", "").TrimEnd();
        }
    }

    public Transform InteractOrigin { get; private set; }
    public bool IsHovering { get; private set; }

    private void Awake()
    {
        var interactOriginObject = new GameObject("Raycast Origin");

        InteractOrigin = interactOriginObject.transform;
        InteractOrigin.SetParent(transform, false);
        InteractOrigin.localPosition = new Vector3(0.01f, 0, 0);
        InteractOrigin.rotation = Quaternion.Euler(80, 0, 0);

        debugLineRenderer = gameObject.AddComponent<LineRenderer>();
        debugLineRenderer.widthCurve.keys = [new Keyframe(0, 1)];
        debugLineRenderer.widthMultiplier = 0.005f;
        debugLineRenderer.positionCount = 2;
        debugLineRenderer.SetPositions([Vector3.zero, Vector3.zero]);
        debugLineRenderer.numCornerVertices = 4;
        debugLineRenderer.numCapVertices = 4;
        debugLineRenderer.alignment = LineAlignment.View;
        debugLineRenderer.shadowBias = 0.5f;
        debugLineRenderer.useWorldSpace = true;
        debugLineRenderer.maskInteraction = SpriteMaskInteraction.None;
        debugLineRenderer.SetMaterials([AssetManager.DefaultRayMat]);
        debugLineRenderer.enabled = Plugin.Config.EnableInteractRay.Value;

        Plugin.Config.EnableInteractRay.SettingChanged +=
            (_, _) => debugLineRenderer.enabled = Plugin.Config.EnableInteractRay.Value;

        // Re-enable local player controller to make sure our "Interact" runs first
        Actions.Instance["Interact"].performed += OnInteractPerformed;
    }

    private void OnDisable()
    {
        IsHovering = false;

        PlayerController.cursorIcon.enabled = false;
        CursorTip = "";
        if (PlayerController.hoveringOverTrigger != null)
            PlayerController.previousHoveringOverTrigger = PlayerController.hoveringOverTrigger;

        PlayerController.hoveringOverTrigger = null;
    }

    private void OnDestroy()
    {
        Actions.Instance["Interact"].performed -= OnInteractPerformed;
    }

    public static void ResetDisabledInteractTriggers()
    {
        DisabledInteractTriggers.Clear();
    }

    public static void EnableInteractTrigger(string objectName)
    {
        DisabledInteractTriggers.Remove(objectName);
    }

    public static void DisableInteractTrigger(string objectName)
    {
        DisabledInteractTriggers.Add(objectName);
    }

    public void ShowDebugInteractorVisual(bool visible = true)
    {
        debugLineRenderer.enabled = visible && Plugin.Config.EnableInteractRay.Value;
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        if (ShipBuildModeManager.Instance.InBuildMode)
            return;

        try
        {
            // Ignore server player controller
            if (!PlayerController.IsOwner ||
                (PlayerController.IsServer && !PlayerController.isHostPlayerObject)) return;

            if (!context.performed) return;
            if (PlayerController.timeSinceSwitchingSlots < 0.2f) return;

            ShipBuildModeManager.Instance.CancelBuildMode();

            if (PlayerController.isGrabbingObjectAnimation || PlayerController.isTypingChat ||
                PlayerController.inTerminalMenu || PlayerController.throwingObject || PlayerController.IsInspectingItem)
                return;

            if (PlayerController.inAnimationWithEnemy != null)
                return;

            if (PlayerController.jetpackControls || PlayerController.disablingJetpackControls)
                return;

            if (StartOfRound.Instance.suckingPlayersOutOfShip)
                return;

            // Handle belt bag item
            if (PlayerController.currentlyHeldObjectServer is BeltBagItem bag && TryBagTool(bag))
                return;

            // Here we try and pickup the item if it's possible
            if (!PlayerController.activatingItem && !VRSession.Instance.LocalPlayer.PlayerController.isPlayerDead)
                BeginGrabObject();

            // WHAT?
            if (PlayerController.hoveringOverTrigger == null || PlayerController.hoveringOverTrigger.holdInteraction ||
                (PlayerController.isHoldingObject && !PlayerController.hoveringOverTrigger.oneHandedItemAllowed) ||
                (PlayerController.twoHanded && (!PlayerController.hoveringOverTrigger.twoHandedItemAllowed ||
                                                PlayerController.hoveringOverTrigger.specialCharacterAnimation)))
                return;

            if (!PlayerController.InteractTriggerUseConditionsMet()) return;

            PlayerController.hoveringOverTrigger.Interact(PlayerController.thisPlayerBody);
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message);
            Debug.LogError(ex.StackTrace);
        }
    }

    private void ClickHoldInteraction()
    {
        var pressed = GrabAction.IsPressed() && !ShipBuildModeManager.Instance.InBuildMode;
        PlayerController.isHoldingInteract = pressed;

        if (!pressed)
        {
            PlayerController.StopHoldInteractionOnTrigger();
            return;
        }

        if (PlayerController.hoveringOverTrigger == null || !PlayerController.hoveringOverTrigger.interactable)
        {
            PlayerController.StopHoldInteractionOnTrigger();
            return;
        }

        if (PlayerController.hoveringOverTrigger == null ||
            !PlayerController.hoveringOverTrigger.gameObject.activeInHierarchy ||
            !PlayerController.hoveringOverTrigger.holdInteraction ||
            PlayerController.hoveringOverTrigger.currentCooldownValue > 0f ||
            (PlayerController.isHoldingObject && !PlayerController.hoveringOverTrigger.oneHandedItemAllowed) ||
            (PlayerController.twoHanded && !PlayerController.hoveringOverTrigger.twoHandedItemAllowed))
        {
            PlayerController.StopHoldInteractionOnTrigger();
            return;
        }

        if (PlayerController.isGrabbingObjectAnimation || PlayerController.isTypingChat ||
            (PlayerController.inSpecialInteractAnimation &&
             !PlayerController.hoveringOverTrigger.allowUseWhileInAnimation) || PlayerController.throwingObject)
        {
            PlayerController.StopHoldInteractionOnTrigger();
            return;
        }

        if (!HUDManager.Instance.HoldInteractionFill(PlayerController.hoveringOverTrigger.timeToHold,
                PlayerController.hoveringOverTrigger.timeToHoldSpeedMultiplier))
        {
            PlayerController.hoveringOverTrigger.HoldInteractNotFilled();
            return;
        }

        PlayerController.hoveringOverTrigger.Interact(PlayerController.thisPlayerBody);
    }

    private void Update()
    {
        if (PlayerController.IsOwner && PlayerController.isPlayerControlled)
            ClickHoldInteraction();
    }

    private void LateUpdate()
    {
        var origin = InteractOrigin.position + InteractOrigin.forward * 0.1f;
        var end = InteractOrigin.position + InteractOrigin.forward * PlayerController.grabDistance;

        debugLineRenderer.SetPositions(new[] { origin, end });

        var shouldReset = true;

        try
        {
            if (PlayerController.isGrabbingObjectAnimation)
                return;

            var ray = new Ray(InteractOrigin.position, InteractOrigin.forward);

            if (!ray.Raycast(out var hit, PlayerController.grabDistance, INTERACTABLE_OBJECTS_MASK) ||
                hit.collider.gameObject.layer == 8 || hit.collider.gameObject.layer == 30)
                return;

            // Place interaction hud on object
            var position = hit.transform.position;
            var offsetComponent = hit.transform.gameObject.GetComponent<InteractCanvasPositionOffset>();
            if (offsetComponent != null)
                position = hit.transform.TransformPoint(offsetComponent.offset);

            if (hit.collider.gameObject.CompareTag("InteractTrigger"))
            {
                var component = hit.transform.gameObject.GetComponent<InteractTrigger>();
                if (component != PlayerController.previousHoveringOverTrigger &&
                    PlayerController.previousHoveringOverTrigger != null)
                {
                    PlayerController.previousHoveringOverTrigger.isBeingHeldByPlayer = false;
                }

                if (component == null || !component.enabled)
                    return;

                // Ignore disabled triggers (like ship lever, charging station, etc)
                if (DisabledInteractTriggers.Contains(component.gameObject.name))
                    return;
                
                if (VRSession.Instance.LocalPlayer.PlayerController.isPlayerDead)
                {
                    // Only ladders and entrance trigger are allowed
                    if (!component.isLadder && hit.transform.gameObject.GetComponent<EntranceTeleport>() == null)
                        return;
                }

                VRSession.Instance.HUD.UpdateInteractCanvasPosition(position);

                if (!IsHovering)
                    VRSession.VibrateController(XRNode.RightHand, 0.1f, 0.2f);

                shouldReset = false;
                IsHovering = true;

                PlayerController.hoveringOverTrigger = component;
                if (!component.interactable)
                {
                    PlayerController.cursorIcon.sprite = component.disabledHoverIcon;
                    PlayerController.cursorIcon.enabled = component.disabledHoverIcon != null;
                    CursorTip = component.disabledHoverTip;
                }
                else if (component.isPlayingSpecialAnimation)
                {
                    PlayerController.cursorIcon.enabled = false;
                    CursorTip = "";
                }
                else if (PlayerController.isHoldingInteract)
                {
                    if (PlayerController.twoHanded) CursorTip = "[Hands full]";
                    else if (!string.IsNullOrEmpty(component.holdTip)) CursorTip = component.holdTip;
                }
                else
                {
                    PlayerController.cursorIcon.enabled = true;
                    PlayerController.cursorIcon.sprite = component.hoverIcon;
                    CursorTip = component.hoverTip;
                }
            }
            else if (hit.collider.gameObject.CompareTag("PhysicsProp"))
            {
                if (VRSession.Instance.LocalPlayer.PlayerController.isPlayerDead)
                    return;

                // Ignore disabled triggers (like ship lever, charging station, etc)
                if (DisabledInteractTriggers.Contains(hit.collider.gameObject.name))
                    return;

                VRSession.Instance.HUD.UpdateInteractCanvasPosition(position);

                if (!IsHovering)
                    VRSession.VibrateController(XRNode.RightHand, 0.1f, 0.2f);

                shouldReset = false;
                IsHovering = true;

                if (PlayerController.FirstEmptyItemSlot() == -1)
                {
                    CursorTip = "Inventory full!";
                }
                else
                {
                    var component = hit.collider.gameObject.GetComponent<GrabbableObject>();

                    if (!GameNetworkManager.Instance.gameHasStarted &&
                        !component.itemProperties.canBeGrabbedBeforeGameStart && StartOfRound.Instance.testRoom == null)
                    {
                        CursorTip = "(Cannot hold until ship has landed)";
                        return;
                    }

                    if (component is null)
                        return;

                    if (PlayerController.currentlyHeldObjectServer is { itemProperties.itemId: 20 } &&
                        CanBeInsertedIntoBag(component))
                        CursorTip = "Store in bag";
                    else if (!string.IsNullOrEmpty(component.customGrabTooltip))
                        CursorTip = component.customGrabTooltip;
                    else
                        CursorTip = "Grab";
                }
            }
        }
        finally
        {
            if (shouldReset)
            {
                IsHovering = false;

                PlayerController.cursorIcon.enabled = false;
                CursorTip = "";
                if (PlayerController.hoveringOverTrigger != null)
                    PlayerController.previousHoveringOverTrigger = PlayerController.hoveringOverTrigger;

                PlayerController.hoveringOverTrigger = null;
            }
        }
    }

    private void BeginGrabObject()
    {
        var ray = new Ray(InteractOrigin.position, InteractOrigin.forward);
        if (!ray.Raycast(out var hit, PlayerController.grabDistance, INTERACTABLE_OBJECTS_MASK) ||
            hit.collider.gameObject.layer == 8 || !hit.collider.CompareTag("PhysicsProp"))
            return;

        if (PlayerController.twoHanded || PlayerController.sinkingValue > 0.73f) return;

        // Ignore disabled triggers (like ship lever, charging station, etc)
        if (DisabledInteractTriggers.Contains(hit.collider.gameObject.name))
            return;

        GrabItem(hit.collider.transform.gameObject.GetComponent<GrabbableObject>());
    }
    
    private bool TryBagTool(BeltBagItem bag)
    {
        var ray = new Ray(InteractOrigin.position, InteractOrigin.forward);
        if (!ray.Raycast(out var hit, 4, 1073742144))
            return false;

        var item = hit.collider.GetComponent<GrabbableObject>();
        if (item == null || item == PlayerController.currentlyHeldObjectServer || !CanBeInsertedIntoBag(item))
            return false;

        bag.TryAddObjectToBag(item);

        return true;
    }

    public void GrabItem(GrabbableObject item)
    {
        PlayerController.currentlyGrabbingObject = item;

        if (!GameNetworkManager.Instance.gameHasStarted &&
            !PlayerController.currentlyGrabbingObject.itemProperties.canBeGrabbedBeforeGameStart &&
            StartOfRound.Instance.testRoom == null)
            return;

        PlayerController.grabInvalidated = false;

        if (PlayerController.currentlyGrabbingObject == null || PlayerController.inSpecialInteractAnimation ||
            PlayerController.currentlyGrabbingObject.isHeld || PlayerController.currentlyGrabbingObject.isPocketed)
            return;

        var networkObject = PlayerController.currentlyGrabbingObject.NetworkObject;
        if (networkObject == null || !networkObject.IsSpawned)
            return;

        PlayerController.currentlyGrabbingObject.InteractItem();
        if (PlayerController.currentlyGrabbingObject.grabbable && PlayerController.FirstEmptyItemSlot() != -1)
        {
            PlayerController.playerBodyAnimator.SetBool(grabInvalidated, false);
            PlayerController.playerBodyAnimator.SetBool(grabValidated, false);
            PlayerController.playerBodyAnimator.SetBool(cancelHolding, false);
            PlayerController.playerBodyAnimator.ResetTrigger(@throw);
            PlayerController.SetSpecialGrabAnimationBool(true);
            PlayerController.isGrabbingObjectAnimation = true;
            PlayerController.cursorIcon.enabled = false;
            CursorTip = "";
            PlayerController.twoHanded = PlayerController.currentlyGrabbingObject.itemProperties.twoHanded;
            PlayerController.carryWeight =
                Mathf.Clamp(
                    PlayerController.carryWeight +
                    (PlayerController.currentlyGrabbingObject.itemProperties.weight - 1f), 1f, 10f);
            PlayerController.grabObjectAnimationTime =
                PlayerController.currentlyGrabbingObject.itemProperties.grabAnimationTime > 0f
                    ? PlayerController.currentlyGrabbingObject.itemProperties.grabAnimationTime
                    : 0.4f;

            if (!PlayerController.isTestingPlayer)
                PlayerController.GrabObjectServerRpc(networkObject);

            if (PlayerController.grabObjectCoroutine != null)
                PlayerController.StopCoroutine(PlayerController.grabObjectCoroutine);

            PlayerController.grabObjectCoroutine = PlayerController.StartCoroutine(PlayerController.GrabObject());
        }
    }

    private static bool CanBeInsertedIntoBag(GrabbableObject item)
    {
        return !item.itemProperties.isScrap && !item.isHeld && !item.isHeldByEnemy &&
               item.itemProperties.itemId is not (123984 or 819501);
    }
}