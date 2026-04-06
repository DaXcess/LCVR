using LCVR.Assets;
using LCVR.Networking;
using LCVR.Player;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using LCVR.Managers;
using UnityEngine;

namespace LCVR.Physics.Interactions;

internal class ShipLeverInteractable : MonoBehaviour, VRInteractable
{
    private ShipLever lever;
    private VRInteractor currentInteractor;
    private VelocityTracker velocityTracker;
    private HashSet<VRInteractor> triggeredHands = new();

    public InteractableFlags Flags => InteractableFlags.BothHands;

    private void Awake()
    {
        lever = GetComponentInParent<ShipLever>();
        
        // Initialize velocity tracker for slap detection
        velocityTracker = gameObject.AddComponent<VelocityTracker>();
        velocityTracker.VelocityThreshold = Plugin.Config.SlapVelocityThreshold.Value;
        velocityTracker.OnVelocityThresholdExceeded += OnSlapDetected;
    }
    
    private void OnSlapDetected(VRInteractor interactor, Vector3 velocity)
    {
        // Check if lever is interactable before processing slap
        if (!lever.CanInteract)
            return;
        
        // Check if we're in the correct ship phase for slapping (only when on moon to depart)
        if (lever.IsInOrbit)
            return;
        
        // Prevent double-trigger from same hand
        if (triggeredHands.Contains(interactor))
            return;
        
        // Mark this hand as having triggered to prevent double-triggers
        triggeredHands.Add(interactor);
        
        // Trigger haptic feedback
        interactor.Vibrate(0.15f, 0.8f);
        
        // Trigger the lever slap action
        lever.TriggerSlapAction(ShipLever.Actor.Self);
        
        // Stop tracking this hand to prevent additional triggers
        velocityTracker.StopTracking(interactor);
    }

    private void Update()
    {
        if (!lever.CanInteract && currentInteractor != null)
            OnButtonRelease(currentInteractor);
    }

    public bool OnButtonPress(VRInteractor interactor)
    {
        if (!lever.CanInteract || Plugin.Config.DisableShipLeverInteraction.Value)
            return false;

        currentInteractor = interactor;

        interactor.FingerCurler.ForceFist(true);

        lever.StartInteracting(interactor.transform, ShipLever.Actor.Self);

        return true;
    }

    public void OnButtonRelease(VRInteractor interactor)
    {
        currentInteractor = null;

        interactor.FingerCurler.ForceFist(false);

        lever.StopInteracting();
    }

    public void OnColliderEnter(VRInteractor interactor)
    {
        if (interactor == null)
            return;
        
        // Check if slap interaction is disabled in config
        if (Plugin.Config.DisableSlapInteraction.Value)
            return;
            
        // Start tracking hand velocity for slap detection
        velocityTracker.StartTracking(interactor);
    }
    
    public void OnColliderExit(VRInteractor interactor)
    {
        if (interactor == null)
            return;
            
        // Clean up double-trigger prevention state
        triggeredHands.Remove(interactor);
        
        // Stop tracking hand velocity when hand leaves collision zone
        velocityTracker.StopTracking(interactor);
    }
}

public class ShipLever : MonoBehaviour
{
    private Animator animator;
    private StartMatchLever lever;
    private Transform rotateTo;
    private TriggerDirection shouldTrigger = TriggerDirection.None;
    private Actor currentActor;
    private Channel channel;

    public bool CanInteract => lever.triggerScript.interactable && currentActor != Actor.Other;
    public bool IsInOrbit => lever.playersManager.inShipPhase;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        lever = FindObjectOfType<StartMatchLever>();
        
        channel = NetworkSystem.Instance.CreateChannel(ChannelType.ShipLever);
        channel.OnPacketReceived += OnOtherInteractWithLever;
    }

    private void Update()
    {
        if (rotateTo == null)
            return;

        var direction = rotateTo.TransformPoint(new Vector3(0.02f, 0.05f, 0f)) - transform.position;
        var targetRotation = Quaternion.LookRotation(direction);
        
        // Constrain rotation to specific axes
        var eulerAngles = targetRotation.eulerAngles;
        
        // Determine if we're pulling down (landing) or pushing forward (departing) based on Y rotation
        bool isPullingDown = eulerAngles.y > 180;
        
        if (isPullingDown)
        {
            // Landing: pulling lever down
            eulerAngles.y = 270;
            eulerAngles.z = 90;
            eulerAngles.x = Mathf.Min(310, eulerAngles.x);
            
            shouldTrigger = eulerAngles.x > 300 ? TriggerDirection.LandShip : TriggerDirection.None;
        }
        else
        {
            // Departing: pushing lever forward
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
        
        if (actor == Actor.Self)
            channel.SendPacket([1]);
    }

    public void StopInteracting()
    {
        try
        {
            if (shouldTrigger == TriggerDirection.LandShip && lever.playersManager.inShipPhase || shouldTrigger == TriggerDirection.DepartShip && !lever.playersManager.inShipPhase)
            {
                if (currentActor == Actor.Self)
                {
                    // Call LeverAnimation first (game needs this to set internal state)
                    lever.LeverAnimation();
                    
                    // Then immediately call PullLever without waiting for animation
                    lever.PullLever();
                }
                
                animator.enabled = true;
                return;
            }

            animator.enabled = true;
        }
        finally
        {
            if (currentActor == Actor.Self)
                channel.SendPacket([0]);
            
            // Always reset at the end
            rotateTo = null;
            shouldTrigger = TriggerDirection.None;
            currentActor = Actor.None;
        }
    }

    public void TriggerSlapAction(Actor actor)
    {
        // Check if lever is interactable
        if (!CanInteract)
            return;

        // Slap should only work when leaving the moon (departing), not landing
        if (IsInOrbit)
            return; // Already in orbit, don't allow slap to land

        currentActor = actor;

        // Start the slap action coroutine
        StartCoroutine(PerformSlapAction(actor == Actor.Self));
    }

    private IEnumerator PerformSlapAction(bool isLocal)
    {
        // Call LeverAnimation to play animation
        if (isLocal)
        {
            lever.LeverAnimation();
            // Send network packet (type 2) for slap synchronization
            channel.SendPacket([2]);
        }

        // Wait 1.67 seconds for animation
        yield return new WaitForSeconds(1.67f);

        // Re-enable animator
        animator.enabled = true;

        // Call PullLever to execute game logic
        if (isLocal)
            lever.PullLever();

        // Reset actor state
        currentActor = Actor.None;
    }

    private void OnOtherInteractWithLever(ushort other, BinaryReader reader)
    {
        var packetType = reader.ReadByte();

        if (!NetworkSystem.Instance.TryGetPlayer(other, out var player))
            return;

        switch (packetType)
        {
            case 0: // Stop interacting (grab release)
                if (currentActor == Actor.Other)
                    StopInteracting();
                break;
            case 1: // Start interacting (grab start)
                if (currentActor == Actor.None)
                    StartInteracting(player.Bones.RightHand, Actor.Other);
                break;
            case 2: // Slap action
                if (currentActor == Actor.None)
                    TriggerSlapAction(Actor.Other);
                break;
        }
    }

    public static void Create()
    {
        var startMatch = FindObjectOfType<StartMatchLever>();
        startMatch.leverAnimatorObject.gameObject.AddComponent<ShipLever>();

        if (!VRSession.InVR)
            return;

        var leverObject = startMatch.leverAnimatorObject.gameObject;
        var interactable = Instantiate(AssetManager.Interactable, leverObject.transform);

        interactable.transform.localPosition = new Vector3(0.2327f, 0.0404f, 11.6164f);
        interactable.transform.localScale = new Vector3(1, 1, 4);

        interactable.AddComponent<ShipLeverInteractable>();
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
