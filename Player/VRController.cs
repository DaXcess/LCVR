using GameNetcodeStuff;
using System;
using System.Collections;
using System.Reflection;
using UnityEngine.InputSystem;
using UnityEngine;
using UnityEngine.XR;
using Unity.Netcode;
using LCVR.Input;
using LCVR.Assets;
using LCVR.UI;
using System.Collections.Generic;

namespace LCVR.Player;

public class VRController : MonoBehaviour
{
    private const int interactableObjectsMask = (1 << 6) | (1 << 8) | (1 << 9);

    private static readonly HashSet<string> disabledInteractTriggers = [];

    private Transform interactOrigin;
    private LineRenderer debugLineRenderer;
    private bool hitInteractable = false;

    private InputAction GrabAction => Actions.Instance["Controls/Interact"];
    private PlayerControllerB PlayerController => VRSession.Instance.LocalPlayer.PlayerController;

    private string CursorTip
    {
        set
        {
            PlayerController.cursorTip.text = value.Replace(": [LMB]", "").Replace(": [RMB]", "").Replace(": [E]", "").TrimEnd();
        }
    }

    private GrabbableObject CurrentlyGrabbingObject
    {
        get
        {
            return GetFieldValue<GrabbableObject>("currentlyGrabbingObject");
        }
        set
        {
            SetFieldValue("currentlyGrabbingObject", value);
        }
    }

    public Transform InteractOrigin => interactOrigin;

    private void Awake()
    {
        var interactOriginObject = new GameObject("Raycast Origin");

        interactOrigin = interactOriginObject.transform;
        interactOrigin.SetParent(transform, false);
        interactOrigin.localPosition = new Vector3(0.01f, 0, 0);
        interactOrigin.rotation = Quaternion.Euler(80, 0, 0);

        debugLineRenderer = gameObject.AddComponent<LineRenderer>();
        debugLineRenderer.widthCurve.keys = [new Keyframe(0, 1)];
        debugLineRenderer.widthMultiplier = 0.005f;
        debugLineRenderer.positionCount = 2;
        debugLineRenderer.SetPositions(new Vector3[] { Vector3.zero, Vector3.zero });
        debugLineRenderer.numCornerVertices = 4;
        debugLineRenderer.numCapVertices = 4;
        debugLineRenderer.alignment = LineAlignment.View;
        debugLineRenderer.shadowBias = 0.5f;
        debugLineRenderer.useWorldSpace = true;
        debugLineRenderer.maskInteraction = SpriteMaskInteraction.None;
        debugLineRenderer.SetMaterials([AssetManager.defaultRayMat]);
        debugLineRenderer.enabled = Plugin.Config.EnableInteractRay.Value;

        Actions.Instance.OnReload += OnActionsReload;
        GrabAction.performed += OnInteractPerformed;
    }

    private void OnActionsReload(InputActionAsset oldActions, InputActionAsset newActions)
    {
        oldActions["Controls/Interact"].performed -= OnInteractPerformed;
        oldActions["Controls/Interact"].performed += OnInteractPerformed;
    }

    private void OnDestroy()
    {
        GrabAction.performed -= OnInteractPerformed;
    }

    public static void EnableInteractTrigger(string objectName)
    {
        disabledInteractTriggers.Remove(objectName);
    }

    public static void DisableInteractTrigger(string objectName)
    {
        disabledInteractTriggers.Add(objectName);
    }

    public void EnableDebugInteractorVisual(bool enabled = true)
    {
        debugLineRenderer.enabled = enabled && Plugin.Config.EnableInteractRay.Value;
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        if (ShipBuildModeManager.Instance.InBuildMode)
            return;

        try
        {
            // Ignore server player controller
            if (!PlayerController.IsOwner || (PlayerController.IsServer && !PlayerController.isHostPlayerObject)) return;

            if (!context.performed) return;
            if (GetFieldValue<float>("timeSinceSwitchingSlots") < 0.2f) return;

            ShipBuildModeManager.Instance.CancelBuildMode(true);

            if (PlayerController.isGrabbingObjectAnimation || PlayerController.isTypingChat || PlayerController.inTerminalMenu || GetFieldValue<bool>("throwingObject") || PlayerController.IsInspectingItem)
                return;

            if (PlayerController.inAnimationWithEnemy != null)
                return;

            if (PlayerController.jetpackControls || PlayerController.disablingJetpackControls)
                return;

            if (StartOfRound.Instance.suckingPlayersOutOfShip)
                return;

            // Here we try and pickup the item if it's is possible
            if (!PlayerController.activatingItem && !VRSession.Instance.LocalPlayer.PlayerController.isPlayerDead)
                BeginGrabObject();

            // WHAT?
            if (PlayerController.hoveringOverTrigger == null || PlayerController.hoveringOverTrigger.holdInteraction || (PlayerController.isHoldingObject && !PlayerController.hoveringOverTrigger.oneHandedItemAllowed) || (PlayerController.twoHanded && (!PlayerController.hoveringOverTrigger.twoHandedItemAllowed || PlayerController.hoveringOverTrigger.specialCharacterAnimation)))
                return;

            if (!InvokeFunction<bool>("InteractTriggerUseConditionsMet")) return;

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
        bool pressed = GrabAction.IsPressed() && !ShipBuildModeManager.Instance.InBuildMode;
        PlayerController.isHoldingInteract = pressed;

        if (!pressed)
        {
            InvokeAction("StopHoldInteractionOnTrigger");
            return;
        }

        if (PlayerController.hoveringOverTrigger == null || !PlayerController.hoveringOverTrigger.interactable)
        {
            InvokeAction("StopHoldInteractionOnTrigger");
            return;
        }

        if (PlayerController.hoveringOverTrigger == null || !PlayerController.hoveringOverTrigger.gameObject.activeInHierarchy || !PlayerController.hoveringOverTrigger.holdInteraction || PlayerController.hoveringOverTrigger.currentCooldownValue > 0f || (PlayerController.isHoldingObject && !PlayerController.hoveringOverTrigger.oneHandedItemAllowed) || (PlayerController.twoHanded && !PlayerController.hoveringOverTrigger.twoHandedItemAllowed))
        {
            InvokeAction("StopHoldInteractionOnTrigger");
            return;
        }

        if (PlayerController.isGrabbingObjectAnimation || PlayerController.isTypingChat || PlayerController.inSpecialInteractAnimation || GetFieldValue<bool>("throwingObject"))
        {
            InvokeAction("StopHoldInteractionOnTrigger");
            return;
        }

        if (!HUDManager.Instance.HoldInteractionFill(PlayerController.hoveringOverTrigger.timeToHold, PlayerController.hoveringOverTrigger.timeToHoldSpeedMultiplier))
        {
            PlayerController.hoveringOverTrigger.HoldInteractNotFilled();
            return;
        }

        PlayerController.hoveringOverTrigger.Interact(PlayerController.thisPlayerBody);
    }

    private void Update()
    {
        if (!PlayerController.inSpecialInteractAnimation || PlayerController.inShockingMinigame || StartOfRound.Instance.suckingPlayersOutOfShip)
        {
            ClickHoldInteraction();
        }
    }

    private void LateUpdate()
    {
        var origin = interactOrigin.position + interactOrigin.forward * 0.1f;
        var end = interactOrigin.position + interactOrigin.forward * PlayerController.grabDistance;

        debugLineRenderer.SetPositions(new Vector3[] { origin, end });

        if (!PlayerController.isGrabbingObjectAnimation)
        {
            var ray = new Ray(interactOrigin.position, interactOrigin.forward);

            if (ray.Raycast(out var hit, PlayerController.grabDistance, interactableObjectsMask) && hit.collider.gameObject.layer != 8)
            {
                // Place interaction hud on object
                var position = hit.transform.position;
                var offsetComponent = hit.transform.gameObject.GetComponent<InteractCanvasPositionOffset>();
                if (offsetComponent != null)
                {
                    position = hit.transform.TransformPoint(offsetComponent.offset);
                }

                if (hit.collider.gameObject.CompareTag("InteractTrigger"))
                {
                    var component = hit.transform.gameObject.GetComponent<InteractTrigger>();
                    if (component != PlayerController.previousHoveringOverTrigger && PlayerController.previousHoveringOverTrigger != null)
                    {
                        PlayerController.previousHoveringOverTrigger.isBeingHeldByPlayer = false;
                    }

                    // Ignore disabled triggers (like ship lever, charging station, etc)
                    if (disabledInteractTriggers.Contains(component.gameObject.name))
                        return;

                    if (VRSession.Instance.LocalPlayer.PlayerController.isPlayerDead)
                    {
                        if (component == null)
                            return;

                        // Only ladders and entrance trigger are allowed
                        if (!component.isLadder && hit.transform.gameObject.GetComponent<EntranceTeleport>() == null)
                            return;
                    }

                    if (component != null)
                    {
                        VRSession.Instance.HUD.UpdateInteractCanvasPosition(position);

                        if (!hitInteractable)
                            VRSession.VibrateController(XRNode.RightHand, 0.1f, 0.2f);

                        hitInteractable = true;

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
                }
                else if (hit.collider.gameObject.CompareTag("PhysicsProp"))
                {
                    if (VRSession.Instance.LocalPlayer.PlayerController.isPlayerDead)
                        return;

                    // Ignore disabled triggers (like ship lever, charging station, etc)
                    if (disabledInteractTriggers.Contains(hit.collider.gameObject.name))
                        return;

                    VRSession.Instance.HUD.UpdateInteractCanvasPosition(position);

                    if (!hitInteractable)
                        VRSession.VibrateController(XRNode.RightHand, 0.1f, 0.2f);

                    hitInteractable = true;

                    if (FirstEmptyItemSlot() == -1)
                    {
                        CursorTip = "Inventory full!";
                    }
                    else
                    {
                        var component = hit.collider.gameObject.GetComponent<GrabbableObject>();

                        if (!GameNetworkManager.Instance.gameHasStarted && !component.itemProperties.canBeGrabbedBeforeGameStart)
                        {
                            CursorTip = "(Cannot hold until ship has landed)";
                            return;
                        }

                        if (component != null && !string.IsNullOrEmpty(component.customGrabTooltip))
                            CursorTip = component.customGrabTooltip;
                        else
                            CursorTip = "Grab";
                    }
                }
            }
            else
            {
                hitInteractable = false;

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
        var ray = new Ray(interactOrigin.position, interactOrigin.forward);
        if (ray.Raycast(out var hit, PlayerController.grabDistance, interactableObjectsMask) && hit.collider.gameObject.layer != 8 && hit.collider.CompareTag("PhysicsProp"))
        {
            if (PlayerController.twoHanded || PlayerController.sinkingValue > 0.73f) return;

            // Ignore disabled triggers (like ship lever, charging station, etc)
            if (disabledInteractTriggers.Contains(hit.collider.gameObject.name))
                return;

            GrabItem(hit.collider.transform.gameObject.GetComponent<GrabbableObject>());
        }
    }

    public void GrabItem(GrabbableObject item)
    {
        CurrentlyGrabbingObject = item;

        if (!GameNetworkManager.Instance.gameHasStarted && !CurrentlyGrabbingObject.itemProperties.canBeGrabbedBeforeGameStart)
            return;

        SetFieldValue("grabInvalidated", false);

        if (CurrentlyGrabbingObject == null || PlayerController.inSpecialInteractAnimation || CurrentlyGrabbingObject.isHeld || CurrentlyGrabbingObject.isPocketed)
            return;

        var networkObject = CurrentlyGrabbingObject.NetworkObject;
        if (networkObject == null || !networkObject.IsSpawned)
            return;

        CurrentlyGrabbingObject.InteractItem();
        if (CurrentlyGrabbingObject.grabbable && FirstEmptyItemSlot() != -1)
        {
            PlayerController.playerBodyAnimator.SetBool("GrabInvalidated", false);
            PlayerController.playerBodyAnimator.SetBool("GrabValidated", false);
            PlayerController.playerBodyAnimator.SetBool("cancelHolding", false);
            PlayerController.playerBodyAnimator.ResetTrigger("Throw");
            InvokeAction("SetSpecialGrabAnimationBool", true, null);
            PlayerController.isGrabbingObjectAnimation = true;
            PlayerController.cursorIcon.enabled = false;
            CursorTip = "";
            PlayerController.twoHanded = CurrentlyGrabbingObject.itemProperties.twoHanded;
            PlayerController.carryWeight += Mathf.Clamp(CurrentlyGrabbingObject.itemProperties.weight - 1f, 0f, 10f);
            if (CurrentlyGrabbingObject.itemProperties.grabAnimationTime > 0f)
                PlayerController.grabObjectAnimationTime = CurrentlyGrabbingObject.itemProperties.grabAnimationTime;
            else
                PlayerController.grabObjectAnimationTime = 0.4f;

            if (!PlayerController.isTestingPlayer)
                InvokeAction("GrabObjectServerRpc", new NetworkObjectReference(networkObject));

            var grabObjectCoroutine = GetFieldValue<Coroutine>("grabObjectCoroutine");
            if (grabObjectCoroutine != null)
            {
                PlayerController.StopCoroutine(grabObjectCoroutine);
            }

            SetFieldValue("grabObjectCoroutine", PlayerController.StartCoroutine(InvokeFunction<IEnumerator>("GrabObject")));
        }
    }

    private int FirstEmptyItemSlot()
    {
        return InvokeFunction<int>("FirstEmptyItemSlot");
    }

    private void InvokeAction(string name, params object[] args)
    {
        typeof(PlayerControllerB).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(PlayerController, args);
    }

    private T InvokeFunction<T>(string name, params object[] args)
    {
        return (T)typeof(PlayerControllerB).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(PlayerController, args);
    }

    private T GetFieldValue<T>(string name)
    {
        return (T)typeof(PlayerControllerB).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(PlayerController);
    }

    private void SetFieldValue<T>(string name, T value)
    {
        typeof(PlayerControllerB).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(PlayerController, value);
    }
}
