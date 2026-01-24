using System.Collections;
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
    private const float MAX_CLIMB_SPEED = 3.0f;
    private const float CLIMB_STRENGTH = 1.0f;

    private InteractTrigger ladderTrigger;

    private Vector3? leftHandGripPoint;
    private Vector3? rightHandGripPoint;

    private VRInteractor leftHandInteractor;
    private VRInteractor rightHandInteractor;

    private bool isActiveLadder;
    private float climbStartTime;

    public InteractableFlags Flags => InteractableFlags.BothHands;

    private void Awake()
    {
        ladderTrigger = GetComponentInParent<InteractTrigger>();
    }

    private void Update()
    {
        if (VRSession.Instance is not { LocalPlayer.PlayerController: var player })
            return;

        if (!player.isClimbingLadder || !isActiveLadder)
            return;

        var totalMovement = Vector3.zero;
        var grippingHands = 0;

        if (leftHandGripPoint.HasValue)
        {
            var leftHand = VRSession.Instance.LocalPlayer.LeftHandVRTarget;
            var worldGripPoint = transform.TransformPoint(leftHandGripPoint.Value);
            var pullVector = worldGripPoint - leftHand.position;
            totalMovement += pullVector;
            grippingHands++;
        }

        if (rightHandGripPoint.HasValue)
        {
            var rightHand = VRSession.Instance.LocalPlayer.RightHandVRTarget;
            var worldGripPoint = transform.TransformPoint(rightHandGripPoint.Value);
            var pullVector = worldGripPoint - rightHand.position;
            totalMovement += pullVector;
            grippingHands++;
        }

        totalMovement *= CLIMB_STRENGTH / grippingHands;
        totalMovement.x = 0;
        totalMovement.z = 0;

        var maxMovementThisFrame = MAX_CLIMB_SPEED * Time.deltaTime;
        totalMovement.y = Mathf.Abs(totalMovement.y) > maxMovementThisFrame 
            ? Mathf.Sign(totalMovement.y) * maxMovementThisFrame 
            : totalMovement.y;

        if (Mathf.Abs(totalMovement.y) > 0.001f)
            player.thisPlayerBody.position += totalMovement;

        if (Time.time - climbStartTime < 0.5f || ladderTrigger.topOfLadderPosition == null)
            return;

        var topY = ladderTrigger.topOfLadderPosition.position.y;
        var playerHeadY = player.gameplayCamera.transform.position.y;

        if (playerHeadY < topY - 0.3f)
            return;

        var exitPosition = ladderTrigger.useRaycastToGetTopPosition
            ? GetRaycastExitPosition(player)
            : ladderTrigger.topOfLadderPosition.position;

        StartCoroutine(ExitLadder(player, exitPosition));
    }

    private Vector3 GetRaycastExitPosition(GameNetcodeStuff.PlayerControllerB player)
    {
        var rayStart = player.transform.position + Vector3.up * 0.5f;
        var rayEnd = ladderTrigger.topOfLadderPosition.position + Vector3.up * 0.5f;

        return UnityEngine.Physics.Linecast(rayStart, rayEnd, out var hit,
            StartOfRound.Instance.collidersAndRoomMaskAndDefault,
            QueryTriggerInteraction.Ignore)
            ? hit.point
            : ladderTrigger.topOfLadderPosition.position;
    }

    private IEnumerator ExitLadder(GameNetcodeStuff.PlayerControllerB player, Vector3 exitPosition)
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
        player.UpdateSpecialAnimationValue(false);

        player.takingFallDamage = false;
        player.fallValue = 0f;
        player.fallValueUncapped = 0f;

        var waitTime = ladderTrigger.animationWaitTime * 0.5f;
        var timer = 0f;
        while (timer <= waitTime)
        {
            yield return null;
            timer += Time.deltaTime;

            player.thisPlayerBody.position = Vector3.Lerp(player.thisPlayerBody.position, exitPosition,
                Mathf.SmoothStep(0f, 1f, timer / waitTime));
            player.thisPlayerBody.rotation = Quaternion.Lerp(player.thisPlayerBody.rotation,
                ladderTrigger.ladderPlayerPositionNode.rotation, Mathf.SmoothStep(0f, 1f, timer / waitTime));
        }

        player.TeleportPlayer(exitPosition);

        ladderTrigger.usingLadder = false;
        ladderTrigger.isPlayingSpecialAnimation = false;
        ladderTrigger.lockedPlayer = null;
    }

    public bool OnButtonPress(VRInteractor interactor)
    {
        if (VRSession.Instance is not { LocalPlayer.PlayerController: var player })
            return false;

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
            if (ladderTrigger == null || !ladderTrigger.interactable)
                return false;

            isActiveLadder = true;
            climbStartTime = Time.time;
            player.isClimbingLadder = true;
            player.thisController.enabled = false;

            player.takingFallDamage = false;
            player.fallValue = 0f;
            player.fallValueUncapped = 0f;
        }
        else if (!isActiveLadder)
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

        if (leftHandGripPoint.HasValue || rightHandGripPoint.HasValue || !isActiveLadder)
            return;

        if (VRSession.Instance is not { LocalPlayer.PlayerController: var player })
            return;

        isActiveLadder = false;
        player.isClimbingLadder = false;
        player.thisController.enabled = true;
        player.inSpecialInteractAnimation = false;
        player.UpdateSpecialAnimationValue(false, 0, 0f, false);

        player.takingFallDamage = false;
        player.fallValue = 0f;
        player.fallValueUncapped = 0f;
    }

    public void OnColliderEnter(VRInteractor interactor) { }
    public void OnColliderExit(VRInteractor interactor) { }
}

// Lightweight wrapper no longer needed with multi-hand InteractionManager support

[LCVRPatch]
[HarmonyPatch]
internal static class LadderPatches
{
    [HarmonyPatch(typeof(InteractTrigger), nameof(InteractTrigger.Start))]
    [HarmonyPostfix]
    private static void OnLadderStart(InteractTrigger __instance)
    {
        if (!__instance.isLadder || Plugin.Config.DisableLadderClimbingInteraction.Value)
            return;

        // Create single collider - InteractionManager now supports multiple hands per interactable
        var collider = Object.Instantiate(AssetManager.Interactable, __instance.transform);

        if (__instance.topOfLadderPosition != null && __instance.bottomOfLadderPosition != null)
        {
            var topPos = __instance.topOfLadderPosition.localPosition;
            var bottomPos = __instance.bottomOfLadderPosition.localPosition;
            var midPoint = (topPos + bottomPos) / 2f;
            var height = Mathf.Abs(topPos.y - bottomPos.y);

            collider.transform.localPosition = midPoint + new Vector3(0f, 0f, 0.3f);
            collider.transform.localScale = new Vector3(1.2f, height, 0.8f);
        }
        else
        {
            collider.transform.localPosition = new Vector3(0f, 0f, 0.3f);
            collider.transform.localScale = new Vector3(1.2f, 3f, 0.8f);
        }

        collider.AddComponent<VRLadder>();

        foreach (var existingCollider in __instance.GetComponents<Collider>())
            existingCollider.enabled = false;
    }

    [HarmonyPatch(typeof(InteractTrigger), nameof(InteractTrigger.Interact))]
    [HarmonyPrefix]
    private static bool PreventLadderInteract(InteractTrigger __instance)
    {
        return !__instance.isLadder || Plugin.Config.DisableLadderClimbingInteraction.Value;
    }
}