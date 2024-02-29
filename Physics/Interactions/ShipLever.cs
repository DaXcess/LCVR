﻿using LCVR.Assets;
using LCVR.Networking;
using LCVR.Player;
using System.Collections;
using UnityEngine;

namespace LCVR.Physics.Interactions;

internal class ShipLeverInteractable : MonoBehaviour, VRInteractable
{
    private ShipLever lever;
    private VRInteractor interactor;

    public InteractableFlags Flags => InteractableFlags.BothHands;

    void Awake()
    {
        lever = GetComponentInParent<ShipLever>();
    }

    void Update()
    {
        if (!lever.CanInteract && interactor != null)
            OnButtonRelease(interactor);
    }

    public bool OnButtonPress(VRInteractor interactor)
    {
        if (!lever.CanInteract || Plugin.Config.DisableShipLeverInteraction.Value)
            return false;

        this.interactor = interactor;

        interactor.FingerCurler.ForceFist(true);

        lever.StartInteracting(interactor.transform, ShipLever.Actor.Self);
        DNet.InteractWithLever(true);

        return true;
    }

    public void OnButtonRelease(VRInteractor interactor)
    {
        this.interactor = null;

        interactor.FingerCurler.ForceFist(false);

        lever.StopInteracting();
        DNet.InteractWithLever(false);
    }

    public void OnColliderEnter(VRInteractor _) { }
    public void OnColliderExit(VRInteractor _) { }
}

public class ShipLever : MonoBehaviour
{
    private Animator animator;
    private StartMatchLever lever;
    private Transform rotateTo;
    private TriggerDirection shouldTrigger = TriggerDirection.None;
    private Actor currentActor;

    public bool CanInteract => lever.triggerScript.interactable && currentActor != Actor.Other;
    public Actor CurrentActor => currentActor;

    void Awake()
    {
        animator = GetComponent<Animator>();
        lever = FindObjectOfType<StartMatchLever>();
    }

    void Update()
    {
        if (rotateTo == null)
            return;

        // There are better ways to do this rotation stuff, I have no clue how though, so this is the best that I could come up with
        var direction = rotateTo.TransformPoint(new Vector3(0.02f, 0.05f, 0f)) - transform.position;
        var eulerAngles = Quaternion.LookRotation(direction).eulerAngles;

        if (eulerAngles.y > 180)
        {
            eulerAngles.y = 270;
            eulerAngles.z = 90;
            eulerAngles.x = Mathf.Min(310, eulerAngles.x);

            shouldTrigger = eulerAngles.x > 300 ? TriggerDirection.LandShip : TriggerDirection.None;
        }
        else
        {
            eulerAngles.y = 90;
            eulerAngles.z = 270;
            eulerAngles.x = Mathf.Min(300, eulerAngles.x);

            shouldTrigger = eulerAngles.x > 290 ? TriggerDirection.DepartShip : TriggerDirection.None;
        }

        transform.eulerAngles = eulerAngles;
    }

    public void StartInteracting(Transform target, Actor actor)
    {
        currentActor = actor;
        animator.enabled = false;
        rotateTo = target;
    }

    public void StopInteracting()
    {
        try
        {
            if (shouldTrigger == TriggerDirection.LandShip && lever.playersManager.inShipPhase || shouldTrigger == TriggerDirection.DepartShip && !lever.playersManager.inShipPhase)
            {
                StartCoroutine(performLeverAction(currentActor == Actor.Self));
                return;
            }

            animator.enabled = true;
        }
        finally
        {
            // Always reset at the end
            rotateTo = null;
            shouldTrigger = TriggerDirection.None;
            currentActor = Actor.None;
        }
    }

    private IEnumerator performLeverAction(bool isLocal)
    {
        if (isLocal) lever.LeverAnimation();

        yield return new WaitForSeconds(1.67f);

        animator.enabled = true;
        if (isLocal) lever.PullLever();
    }

    public static ShipLever Create()
    {
        var startMatch = FindObjectOfType<StartMatchLever>();
        var lever = startMatch.leverAnimatorObject.gameObject.AddComponent<ShipLever>();

        if (VRSession.InVR)
        {
            var leverObject = startMatch.leverAnimatorObject.gameObject;
            var interactable = Instantiate(AssetManager.interactable, leverObject.transform);

            interactable.transform.localPosition = new Vector3(0.2327f, 0.0404f, 11.6164f);
            interactable.transform.localScale = new Vector3(1, 1, 4);

            interactable.AddComponent<ShipLeverInteractable>();
        }

        return lever;
    }

    public enum Actor
    {
        None,
        Self,
        Other
    }

    private enum TriggerDirection
    {
        None,
        LandShip,
        DepartShip
    }
}
