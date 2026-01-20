using HarmonyLib;
using LCVR.Assets;
using LCVR.Managers;
using LCVR.Patches;
using LCVR.Player;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LCVR.Physics.Interactions;

public class VRLadder : MonoBehaviour, VRInteractable
{
    private InteractTrigger ladderTrigger;
    
    private Vector3? leftHandGripPoint;
    private Vector3? rightHandGripPoint;
    
    private VRInteractor leftHandInteractor;
    private VRInteractor rightHandInteractor;
    
    private bool isActiveLadder;
    private float climbStartTime;
    
    private const float MAX_CLIMB_SPEED = 3.0f;
    private const float CLIMB_STRENGTH = 1.0f;
    
    public InteractableFlags Flags => InteractableFlags.BothHands;

    private void Awake()
    {
        ladderTrigger = GetComponentInParent<InteractTrigger>();
    }

    private void Update()
    {
        if (VRSession.Instance?.LocalPlayer?.PlayerController == null)
            return;
            
        var player = VRSession.Instance.LocalPlayer.PlayerController;
        
        if (!player.isClimbingLadder || !isActiveLadder)
            return;

        Vector3 totalMovement = Vector3.zero;
        int grippingHands = 0;

        if (leftHandGripPoint.HasValue)
        {
            var leftHand = VRSession.Instance.LocalPlayer.LeftHandVRTarget;
            var worldGripPoint = transform.TransformPoint(leftHandGripPoint.Value);
            Vector3 pullVector = worldGripPoint - leftHand.position;
            totalMovement += pullVector;
            grippingHands++;
        }

        if (rightHandGripPoint.HasValue)
        {
            var rightHand = VRSession.Instance.LocalPlayer.RightHandVRTarget;
            var worldGripPoint = transform.TransformPoint(rightHandGripPoint.Value);
            Vector3 pullVector = worldGripPoint - rightHand.position;
            totalMovement += pullVector;
            grippingHands++;
        }

        if (grippingHands > 1)
            totalMovement /= grippingHands;

        totalMovement *= CLIMB_STRENGTH;
        totalMovement.x = 0;
        totalMovement.z = 0;

        float maxMovementThisFrame = MAX_CLIMB_SPEED * Time.deltaTime;
        if (Mathf.Abs(totalMovement.y) > maxMovementThisFrame)
            totalMovement.y = Mathf.Sign(totalMovement.y) * maxMovementThisFrame;

        if (Mathf.Abs(totalMovement.y) > 0.001f)
        {
            player.thisPlayerBody.position += totalMovement;
        }
        
        if (Time.time - climbStartTime >= 0.5f && ladderTrigger.topOfLadderPosition != null)
        {
            var topY = ladderTrigger.topOfLadderPosition.position.y;
            var playerHeadY = player.gameplayCamera.transform.position.y;
            
            if (playerHeadY >= topY - 0.3f)
            {
                Vector3 exitPosition;
                
                if (ladderTrigger.useRaycastToGetTopPosition)
                {
                    var rayStart = player.transform.position + Vector3.up * 0.5f;
                    var rayEnd = ladderTrigger.topOfLadderPosition.position + Vector3.up * 0.5f;
                    
                    if (UnityEngine.Physics.Linecast(rayStart, rayEnd, out RaycastHit hit, 
                                        StartOfRound.Instance.collidersAndRoomMaskAndDefault, 
                                        QueryTriggerInteraction.Ignore))
                    {
                        exitPosition = hit.point;
                    }
                    else
                    {
                        exitPosition = ladderTrigger.topOfLadderPosition.position;
                    }
                }
                else
                {
                    exitPosition = ladderTrigger.topOfLadderPosition.position;
                }
                
                ExitLadder(player, exitPosition);
            }
        }
    }
    
    private void ExitLadder(GameNetcodeStuff.PlayerControllerB player, Vector3 exitPosition)
    {
        leftHandGripPoint = null;
        rightHandGripPoint = null;
        
        if (leftHandInteractor != null)
        {
            leftHandInteractor.FingerCurler.ForceFist(false);
            leftHandInteractor.isHeld = false;
            leftHandInteractor = null;
        }
        if (rightHandInteractor != null)
        {
            rightHandInteractor.FingerCurler.ForceFist(false);
            rightHandInteractor.isHeld = false;
            rightHandInteractor = null;
        }
        
        isActiveLadder = false;
        
        player.isClimbingLadder = false;
        player.thisController.enabled = true;
        player.inSpecialInteractAnimation = false;
        player.UpdateSpecialAnimationValue(false, 0, 0f, false);
        
        player.takingFallDamage = false;
        player.fallValue = 0f;
        player.fallValueUncapped = 0f;
        
        player.TeleportPlayer(exitPosition, false, 0f, false, true);
        
        ladderTrigger.usingLadder = false;
        ladderTrigger.isPlayingSpecialAnimation = false;
        ladderTrigger.lockedPlayer = null;
    }

    public bool OnButtonPress(VRInteractor interactor)
    {
        var player = VRSession.Instance.LocalPlayer.PlayerController;
        
        // Store grip point in ladder's local space
        if (interactor.IsRightHand)
        {
            rightHandGripPoint = transform.InverseTransformPoint(VRSession.Instance.LocalPlayer.RightHandVRTarget.position);
            rightHandInteractor = interactor;
        }
        else
        {
            leftHandGripPoint = transform.InverseTransformPoint(VRSession.Instance.LocalPlayer.LeftHandVRTarget.position);
            leftHandInteractor = interactor;
        }
        
        if (!player.isClimbingLadder)
        {
            if (ladderTrigger != null && ladderTrigger.interactable)
            {
                isActiveLadder = true;
                climbStartTime = Time.time;
                player.isClimbingLadder = true;
                player.thisController.enabled = false;
                
                player.takingFallDamage = false;
                player.fallValue = 0f;
                player.fallValueUncapped = 0f;
            }
            else
            {
                return false;
            }
        }
        else if (player.isClimbingLadder && !isActiveLadder)
        {
            return false;
        }

        interactor.FingerCurler.ForceFist(true);
        return true;
    }

    public void OnButtonRelease(VRInteractor interactor)
    {
        if (interactor.IsRightHand)
        {
            rightHandGripPoint = null;
            rightHandInteractor = null;
        }
        else
        {
            leftHandGripPoint = null;
            leftHandInteractor = null;
        }

        interactor.FingerCurler.ForceFist(false);

        if (!leftHandGripPoint.HasValue && !rightHandGripPoint.HasValue && isActiveLadder)
        {
            var player = VRSession.Instance.LocalPlayer.PlayerController;
            
            isActiveLadder = false;
            player.isClimbingLadder = false;
            player.thisController.enabled = true;
            player.inSpecialInteractAnimation = false;
            player.UpdateSpecialAnimationValue(false, 0, 0f, false);
            
            player.takingFallDamage = false;
            player.fallValue = 0f;
            player.fallValueUncapped = 0f;
        }
    }

    public void OnColliderEnter(VRInteractor interactor) { }
    public void OnColliderExit(VRInteractor interactor) { }
}

// Lightweight wrapper that forwards to the shared ladder component
internal class VRLadderInteractable : MonoBehaviour, VRInteractable
{
    public VRLadder ladder;
    
    public InteractableFlags Flags => InteractableFlags.BothHands;
    
    public bool OnButtonPress(VRInteractor interactor) => ladder.OnButtonPress(interactor);
    public void OnButtonRelease(VRInteractor interactor) => ladder.OnButtonRelease(interactor);
    public void OnColliderEnter(VRInteractor interactor) { }
    public void OnColliderExit(VRInteractor interactor) { }
}

[LCVRPatch]
[HarmonyPatch]
internal static class LadderPatches
{
    [HarmonyPatch(typeof(InteractTrigger), nameof(InteractTrigger.Start))]
    [HarmonyPostfix]
    private static void OnLadderStart(InteractTrigger __instance)
    {
        if (!__instance.isLadder)
            return;

        if (Plugin.Config.DisableLadderClimbingInteraction.Value)
            return;

        var ladderComponent = __instance.gameObject.AddComponent<VRLadder>();

        // Create two separate colliders offset to left and right
        // This allows both hands to interact simultaneously
        var leftHandCollider = Object.Instantiate(AssetManager.Interactable, __instance.transform);
        var rightHandCollider = Object.Instantiate(AssetManager.Interactable, __instance.transform);
        
        if (__instance.topOfLadderPosition != null && __instance.bottomOfLadderPosition != null)
        {
            var topPos = __instance.topOfLadderPosition.localPosition;
            var bottomPos = __instance.bottomOfLadderPosition.localPosition;
            var midPoint = (topPos + bottomPos) / 2f;
            var height = Mathf.Abs(topPos.y - bottomPos.y);
            
            // Offset left collider to the left side
            leftHandCollider.transform.localPosition = midPoint + new Vector3(-0.3f, 0f, 0.3f);
            leftHandCollider.transform.localScale = new Vector3(0.8f, height, 0.8f);
            
            // Offset right collider to the right side
            rightHandCollider.transform.localPosition = midPoint + new Vector3(0.3f, 0f, 0.3f);
            rightHandCollider.transform.localScale = new Vector3(0.8f, height, 0.8f);
        }
        else
        {
            leftHandCollider.transform.localPosition = new Vector3(-0.3f, 0f, 0.3f);
            leftHandCollider.transform.localScale = new Vector3(0.8f, 3f, 0.8f);
            
            rightHandCollider.transform.localPosition = new Vector3(0.3f, 0f, 0.3f);
            rightHandCollider.transform.localScale = new Vector3(0.8f, 3f, 0.8f);
        }
        
        // Both colliders reference the same ladder component
        leftHandCollider.AddComponent<VRLadderInteractable>().ladder = ladderComponent;
        rightHandCollider.AddComponent<VRLadderInteractable>().ladder = ladderComponent;
        
        foreach (var collider in __instance.GetComponents<Collider>())
            collider.enabled = false;
    }
    
    [HarmonyPatch(typeof(InteractTrigger), nameof(InteractTrigger.Interact))]
    [HarmonyPrefix]
    private static bool PreventLadderInteract(InteractTrigger __instance)
    {
        if (!__instance.isLadder)
            return true;
            
        if (Plugin.Config.DisableLadderClimbingInteraction.Value)
            return true;
        
        return false;
    }
}
