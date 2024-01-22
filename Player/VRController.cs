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
using System.Linq;
using LCVR.UI;

namespace LCVR.Player
{
    internal class VRController : MonoBehaviour
    {
        private const int interactableObjectsMask = 832;

        private InputAction grabAction;

        public VRPlayer player;
        public PlayerControllerB playerController;

        public Transform interactOrigin;
        public MotionDetector motionDetector;

        public LineRenderer debugLineRenderer;

        private bool hitInteractable = false;

        private string cursorTip
        {
            get
            {
                return playerController.cursorTip.text;
            }
            set
            {
                playerController.cursorTip.text = value.Replace(": [LMB]", "").Replace(": [RMB]", "").Replace(": [E]", "").TrimEnd();
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

        private void Awake()
        {
            grabAction = Actions.FindAction("Controls/Interact");
            grabAction.performed += OnInteractPerformed;
        }

        private void OnDestroy()
        {
            grabAction.performed -= OnInteractPerformed;
        }

        public void Initialize(VRPlayer player)
        {
            this.player = player;
            playerController = player.gameObject.GetComponent<PlayerControllerB>();

            var interactOriginObject = new GameObject("Raycast Origin");

            motionDetector = player.rightController.AddComponent<MotionDetector>();

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
        }

        public void ShowDebugLineRenderer()
        {
            debugLineRenderer.enabled = Plugin.Config.EnableInteractRay.Value;
        }

        public void HideDebugLineRenderer()
        {
            debugLineRenderer.enabled = false;
        }

        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            if (ShipBuildModeManager.Instance.InBuildMode)
                return;

            try
            {
                // Ignore server player controller
                if (!playerController.IsOwner || (playerController.IsServer && !playerController.isHostPlayerObject)) return;

                // If we are dead use this as spectator toggle
                if (playerController.isPlayerDead)
                {
                    if (StartOfRound.Instance.overrideSpectateCamera)
                        return;

                    if (playerController.spectatedPlayerScript != null && !playerController.spectatedPlayerScript.isPlayerDead)
                        InvokeAction("SpectateNextPlayer");

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

                // Here we try and pickup the item if it's is possible
                if (!playerController.activatingItem)
                    BeginGrabObject();

                // WHAT?
                if (playerController.hoveringOverTrigger == null || playerController.hoveringOverTrigger.holdInteraction || (playerController.isHoldingObject && !playerController.hoveringOverTrigger.oneHandedItemAllowed) || (playerController.twoHanded && (!playerController.hoveringOverTrigger.twoHandedItemAllowed || playerController.hoveringOverTrigger.specialCharacterAnimation)))
                    return;

                if (!InvokeFunction<bool>("InteractTriggerUseConditionsMet")) return;

                playerController.hoveringOverTrigger.Interact(playerController.thisPlayerBody);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
                Debug.LogError(ex.StackTrace);
            }
        }

        private void ClickHoldInteraction()
        {
            bool pressed = grabAction.IsPressed() && !ShipBuildModeManager.Instance.InBuildMode;
            playerController.isHoldingInteract = pressed;

            if (!pressed)
            {
                InvokeAction("StopHoldInteractionOnTrigger");
                return;
            }

            if (playerController.hoveringOverTrigger == null || !playerController.hoveringOverTrigger.interactable)
            {
                InvokeAction("StopHoldInteractionOnTrigger");
                return;
            }

            if (playerController.hoveringOverTrigger == null || !playerController.hoveringOverTrigger.gameObject.activeInHierarchy || !playerController.hoveringOverTrigger.holdInteraction || playerController.hoveringOverTrigger.currentCooldownValue > 0f || (playerController.isHoldingObject && !playerController.hoveringOverTrigger.oneHandedItemAllowed) || (playerController.twoHanded && !playerController.hoveringOverTrigger.twoHandedItemAllowed))
            {
                InvokeAction("StopHoldInteractionOnTrigger");
                return;
            }

            if (playerController.isGrabbingObjectAnimation || playerController.isTypingChat || playerController.inSpecialInteractAnimation || GetFieldValue<bool>("throwingObject"))
            {
                InvokeAction("StopHoldInteractionOnTrigger");
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
            var origin = interactOrigin.position + interactOrigin.forward * 0.1f;
            var end = interactOrigin.position + interactOrigin.forward * playerController.grabDistance;

            debugLineRenderer.SetPositions(new Vector3[] { origin, end });

            if (!playerController.isGrabbingObjectAnimation)
            {
                var ray = new Ray(interactOrigin.position, interactOrigin.forward);

                if (ray.BoxCast(0.1f, out var hit, playerController.grabDistance, interactableObjectsMask) && hit.collider.gameObject.layer != 8)
                {
                    if (!hitInteractable)
                        VRPlayer.VibrateController(XRNode.RightHand, 0.1f, 0.2f);

                    hitInteractable = true;

                    // Place interaction hud on object
                    var position = hit.transform.position;
                    var offsetComponent = hit.transform.gameObject.GetComponent<InteractCanvasPositionOffset>();
                    if (offsetComponent != null)
                    {
                        position = hit.transform.TransformPoint(offsetComponent.offset);
                    }
                    
                    player.hud.UpdateInteractCanvasPosition(position);

                    if (hit.collider.gameObject.CompareTag("InteractTrigger"))
                    {
                        var component = hit.transform.gameObject.GetComponent<InteractTrigger>();
                        if (component != playerController.previousHoveringOverTrigger && playerController.previousHoveringOverTrigger != null)
                        {
                            playerController.previousHoveringOverTrigger.isBeingHeldByPlayer = false;
                        }

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

                            if (!GameNetworkManager.Instance.gameHasStarted && !component.itemProperties.canBeGrabbedBeforeGameStart)
                            {
                                cursorTip = "(Cannot hold until ship has landed)";
                                return;
                            }

                            if (Items.unsupportedItems.Contains(component.itemProperties.itemName))
                            {
                                cursorTip = "(This item cannot be used in VR)";
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
                    hitInteractable = false;

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
            var ray = new Ray(interactOrigin.position, interactOrigin.forward);
            if (ray.BoxCast(0.1f, out var hit, playerController.grabDistance, interactableObjectsMask) && hit.collider.gameObject.layer != 8 && hit.collider.CompareTag("PhysicsProp"))
            {
                if (playerController.twoHanded || playerController.sinkingValue > 0.73f) return;

                currentlyGrabbingObject = hit.collider.transform.gameObject.GetComponent<GrabbableObject>();

                if (!GameNetworkManager.Instance.gameHasStarted && !currentlyGrabbingObject.itemProperties.canBeGrabbedBeforeGameStart)
                    return;

                if (Items.unsupportedItems.Contains(currentlyGrabbingObject.itemProperties.itemName))
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
                    InvokeAction("SetSpecialGrabAnimationBool", true, null);
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
                        InvokeAction("GrabObjectServerRpc", new NetworkObjectReference(networkObject));

                    var grabObjectCoroutine = GetFieldValue<Coroutine>("grabObjectCoroutine");
                    if (grabObjectCoroutine != null)
                    {
                        playerController.StopCoroutine(grabObjectCoroutine);
                    }

                    SetFieldValue("grabObjectCoroutine", playerController.StartCoroutine(InvokeFunction<IEnumerator>("GrabObject")));
                }
            }
        }

        private int FirstEmptyItemSlot()
        {
            return InvokeFunction<int>("FirstEmptyItemSlot");
        }

        private void InvokeAction(string name, params object[] args)
        {
            typeof(PlayerControllerB).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(playerController, args);
        }

        private T InvokeFunction<T>(string name, params object[] args)
        {
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
