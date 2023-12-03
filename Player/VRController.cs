using GameNetcodeStuff;
using System;
using System.Collections;
using System.Reflection;
using UnityEngine.InputSystem;
using UnityEngine;
using UnityEngine.XR;

namespace LethalCompanyVR
{
    internal class VRController : MonoBehaviour
    {
        private const int interactableObjectsMask = 832;

        public VRPlayer player;
        public PlayerControllerB playerController;

        public GameObject raycastOrigin;

        private string cursorTip
        {
            get
            {
                return playerController.cursorTip.text;
            }
            set
            {
                Logger.LogDebug($"Setting text: {value}");
                playerController.cursorTip.text = value;
            }
        }

        private GrabbableObject currentlyGrabbingObject
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

        public void Initialize(VRPlayer player)
        {
            this.player = player;
            this.playerController = player.gameObject.GetComponent<PlayerControllerB>();
            this.raycastOrigin = new GameObject("Raycast Origin");

            this.raycastOrigin.transform.SetParent(transform, false);
            this.raycastOrigin.transform.rotation = Quaternion.Euler(45, 0, 0);

            Actions.XR_RightHand_Grip_Button.performed += OnGrabPerformed;
        }

        private void OnGrabPerformed(InputAction.CallbackContext context)
        {
            try
            {
                // Ignore server player controller
                if (!playerController.IsOwner || (playerController.IsServer && !playerController.isHostPlayerObject)) return;

                // If we are dead use this as spectator toggle
                if (playerController.isPlayerDead)
                {
                    if (StartOfRound.Instance.overrideSpectateCamera)
                    {
                        return;
                    }

                    if (playerController.spectatedPlayerScript != null && !playerController.spectatedPlayerScript.isPlayerDead)
                    {
                        InvokeFunction<object>("SpectateNextPlayer");
                    }

                    return;
                }

                if (!context.performed) return;
                if (GetFieldValue<float>("timeSinceSwitchingSlots") < 0.2f) return;

                ShipBuildModeManager.Instance.CancelBuildMode(true);

                if (playerController.isGrabbingObjectAnimation || playerController.isTypingChat || playerController.inTerminalMenu || GetFieldValue<bool>("throwingObject") || playerController.IsInspectingItem)
                    return;

                if (playerController.inAnimationWithEnemy != null)
                    return;

                if (playerController.jetpackControls || playerController.disablingJetpackControls)
                    return;

                if (StartOfRound.Instance.suckingPlayersOutOfShip)
                    return;

                // Here we try and pickup the item if that is possible
                if (!playerController.activatingItem)
                {
                    BeginGrabObject();
                }

                // WHAT?
                if (playerController.hoveringOverTrigger == null || playerController.hoveringOverTrigger.holdInteraction || (playerController.isHoldingObject && !playerController.hoveringOverTrigger.oneHandedItemAllowed) || (playerController.twoHanded && (!playerController.hoveringOverTrigger.twoHandedItemAllowed || playerController.hoveringOverTrigger.specialCharacterAnimation)))
                    return;

                if (!InvokeFunction<bool>("InteractTriggerUseConditionsMet")) return;

                playerController.hoveringOverTrigger.Interact(playerController.thisPlayerBody);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.StackTrace);
            }
        }

        private void ClickHoldInteraction()
        {
            bool pressed = Actions.XR_RightHand_Grip_Button.IsPressed();
            playerController.isHoldingInteract = pressed;

            if (!pressed)
            {
                InvokeFunction<object>("StopHoldInteractionOnTrigger");
                return;
            }

            if (playerController.hoveringOverTrigger == null || !playerController.hoveringOverTrigger.interactable)
            {
                InvokeFunction<object>("StopHoldInteractionOnTrigger");
                return;
            }

            if (playerController.hoveringOverTrigger == null || !playerController.hoveringOverTrigger.gameObject.activeInHierarchy || !playerController.hoveringOverTrigger.holdInteraction || playerController.hoveringOverTrigger.currentCooldownValue > 0f || (playerController.isHoldingObject && !playerController.hoveringOverTrigger.oneHandedItemAllowed) || (playerController.twoHanded && !playerController.hoveringOverTrigger.twoHandedItemAllowed))
            {
                InvokeFunction<object>("StopHoldInteractionOnTrigger");
                return;
            }

            if (playerController.isGrabbingObjectAnimation || playerController.isTypingChat || playerController.inSpecialInteractAnimation || GetFieldValue<bool>("throwingObject"))
            {
                InvokeFunction<object>("StopHoldInteractionOnTrigger");
                return;
            }

            if (!HUDManager.Instance.HoldInteractionFill(playerController.hoveringOverTrigger.timeToHold, playerController.hoveringOverTrigger.timeToHoldSpeedMultiplier))
            {
                playerController.hoveringOverTrigger.HoldInteractNotFilled();
                return;
            }

            playerController.hoveringOverTrigger.Interact(playerController.thisPlayerBody);
        }

        private void Update()
        {
            if (!playerController.inSpecialInteractAnimation || playerController.inShockingMinigame || StartOfRound.Instance.suckingPlayersOutOfShip)
            {
                ClickHoldInteraction();
            }
        }

        private void LateUpdate()
        {
            if (!playerController.isGrabbingObjectAnimation)
            {
                var ray = new Ray(raycastOrigin.transform.position, raycastOrigin.transform.forward);

                if (Physics.Raycast(ray, out var hit, playerController.grabDistance, interactableObjectsMask) && hit.collider.gameObject.layer != 8)
                {
                    Logger.LogDebug($"[RAYCAST] {name}: {hit.collider.gameObject.name} - [{hit.collider.gameObject.tag}]");

                    if (playerController.hoveringOverTrigger == null)
                        VRPlayer.VibrateController(XRNode.RightHand, 0.1f, 0.2f);

                    if (hit.collider.gameObject.CompareTag("InteractTrigger"))
                    {
                        var component = hit.transform.gameObject.GetComponent<InteractTrigger>();
                        if (component != playerController.previousHoveringOverTrigger && playerController.previousHoveringOverTrigger != null)
                        {
                            playerController.previousHoveringOverTrigger.isBeingHeldByPlayer = false;
                        }

                        Logger.LogDebug(component);

                        if (component != null)
                        {
                            playerController.hoveringOverTrigger = component;
                            if (!component.interactable)
                            {
                                playerController.cursorIcon.sprite = component.disabledHoverIcon;
                                playerController.cursorIcon.enabled = component.disabledHoverIcon != null;
                                cursorTip = component.disabledHoverTip;
                            }
                            else if (component.isPlayingSpecialAnimation)
                            {
                                playerController.cursorIcon.enabled = false;
                                cursorTip = "";
                            }
                            else if (playerController.isHoldingInteract)
                            {
                                if (playerController.twoHanded) cursorTip = "[Hands full]";
                                else if (!string.IsNullOrEmpty(component.holdTip)) cursorTip = component.holdTip;
                            }
                            else
                            {
                                playerController.cursorIcon.enabled = true;
                                playerController.cursorIcon.sprite = component.hoverIcon;
                                cursorTip = component.hoverTip;
                            }
                        }
                    }
                    else if (hit.collider.gameObject.CompareTag("PhysicsProp"))
                    {
                        if (FirstEmptyItemSlot() == -1)
                        {
                            cursorTip = "Inventory full!";
                        }
                        else
                        {
                            var component = hit.collider.gameObject.GetComponent<GrabbableObject>();
                            if (!GameNetworkManager.Instance.gameHasStarted && !component.itemProperties.canBeGrabbedBeforeGameStart && !StartOfRound.Instance.testRoom.activeSelf)
                            {
                                cursorTip = "(Cannot hold until ship has landed)";
                                return;
                            }

                            if (component != null && !string.IsNullOrEmpty(component.customGrabTooltip))
                                cursorTip = component.customGrabTooltip;
                            else
                                cursorTip = "Grab";
                        }
                    }
                }
                else
                {
                    playerController.cursorIcon.enabled = false;
                    cursorTip = "";
                    if (playerController.hoveringOverTrigger != null)
                        playerController.previousHoveringOverTrigger = playerController.hoveringOverTrigger;

                    playerController.hoveringOverTrigger = null;
                }
            }
        }

        private void BeginGrabObject()
        {
            var ray = new Ray(raycastOrigin.transform.position, raycastOrigin.transform.forward);
            if (Physics.Raycast(ray, out var hit, playerController.grabDistance, interactableObjectsMask) && hit.collider.gameObject.layer != 8 && hit.collider.CompareTag("PhysicsProp"))
            {
                if (playerController.twoHanded || playerController.sinkingValue > 0.73f) return;

                SetFieldValue("currentlyGrabbingObject", hit.collider.transform.gameObject.GetComponent<GrabbableObject>());

                if (!GameNetworkManager.Instance.gameHasStarted && !currentlyGrabbingObject.itemProperties.canBeGrabbedBeforeGameStart && !StartOfRound.Instance.testRoom.activeSelf)
                    return;

                SetFieldValue("grabInvalidated", false);

                if (currentlyGrabbingObject == null || playerController.inSpecialInteractAnimation || currentlyGrabbingObject.isHeld || currentlyGrabbingObject.isPocketed)
                    return;

                var networkObject = currentlyGrabbingObject.NetworkObject;
                if (networkObject == null || !networkObject.IsSpawned)
                    return;

                currentlyGrabbingObject.InteractItem();
                if (currentlyGrabbingObject.grabbable && FirstEmptyItemSlot() != -1)
                {
                    playerController.playerBodyAnimator.SetBool("GrabInvalidated", false);
                    playerController.playerBodyAnimator.SetBool("GrabValidated", false);
                    playerController.playerBodyAnimator.SetBool("cancelHolding", false);
                    playerController.playerBodyAnimator.ResetTrigger("Throw");
                    InvokeFunction<object>("SetSpecialGrabAnimationBool", true, null);
                    playerController.isGrabbingObjectAnimation = true;
                    playerController.cursorIcon.enabled = false;
                    cursorTip = "";
                    playerController.twoHanded = currentlyGrabbingObject.itemProperties.twoHanded;
                    playerController.carryWeight += Mathf.Clamp(currentlyGrabbingObject.itemProperties.weight - 1f, 0f, 10f);
                    if (currentlyGrabbingObject.itemProperties.grabAnimationTime > 0f)
                        playerController.grabObjectAnimationTime = currentlyGrabbingObject.itemProperties.grabAnimationTime;
                    else
                        playerController.grabObjectAnimationTime = 0.4f;

                    if (!playerController.isTestingPlayer)
                        InvokeFunction<object>("GrabObjectServerRpc", networkObject);

                    var grabObjectCoroutine = GetFieldValue<Coroutine>("grabObjectCoroutine");
                    if (grabObjectCoroutine != null)
                    {
                        playerController.StopCoroutine(grabObjectCoroutine);
                    }

                    SetFieldValue("grabObjectCoroutine", playerController.StartCoroutine((IEnumerator)InvokeFunction<IEnumerable>("GrabObject")));
                }
            }
        }

        private int FirstEmptyItemSlot()
        {
            return InvokeFunction<int>("FirstEmptyItemSlot");
        }

        private T InvokeFunction<T>(string name, params object[] args)
        {
            Logger.LogDebug($"Invoking function: {name} ({args.Length} arg{(args.Length == 1 ? "" : "s")})");

            return (T)typeof(PlayerControllerB).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(playerController, args);
        }

        private T GetFieldValue<T>(string name)
        {
            return (T)typeof(PlayerControllerB).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(playerController);
        }

        private void SetFieldValue<T>(string name, T value)
        {
            typeof(PlayerControllerB).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(playerController, value);
        }
    }
}
