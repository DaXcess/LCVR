using LCVR.Assets;
using LCVR.Networking;
using LCVR.Player;
using System.Collections;
using System.IO;
using LCVR.Input;
using LCVR.Managers;
using UnityEngine;

namespace LCVR.Physics.Interactions;

public class ShipLeverInteractable : MonoBehaviour, VRInteractable
{
    private ShipLever lever;
    private VRInteractor currentInteractor;

    public InteractableFlags Flags => InteractableFlags.BothHands;

    private void Awake()
    {
        lever = GetComponentInParent<ShipLever>();
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

        interactor.SnapTo(transform, rotationOffset: new Vector3(0, 0, -90));
        interactor.FingerCurler.ForceFist(true);

        lever.StartInteracting(interactor.TrackedController, ShipLever.Actor.Self);

        return true;
    }

    public void OnButtonRelease(VRInteractor interactor)
    {
        currentInteractor = null;

        interactor.SnapTo(null);
        interactor.FingerCurler.ForceFist(false);

        lever.StopInteracting();
    }

    public void OnColliderEnter(VRInteractor interactor)
    {
        if (lever.InOrbit || !lever.CanInteract)
            return;

        var velocity = interactor.IsRightHand
            ? Actions.Instance.RightHandVelocity.ReadValue<Vector3>()
            : Actions.Instance.LeftHandVelocity.ReadValue<Vector3>();

        if (velocity.sqrMagnitude < 1f)
            return;

        lever.ShoveLever();
    }

    public void OnColliderExit(VRInteractor _) { }
}

public class ShipLever : MonoBehaviour
{
    private static readonly int shoveLever = Animator.StringToHash("shoveLever");
    
    private Animator animator;
    private StartMatchLever lever;
    private Transform rotateTo;
    private TriggerDirection shouldTrigger = TriggerDirection.None;
    private Actor currentActor;
    private Channel channel;

    public bool CanInteract => currentActor != Actor.Other && lever.triggerScript.interactable;
    public bool InOrbit => lever.playersManager.inShipPhase;

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

    public void ShoveLever(bool isOwner = true)
    {
        // Can only be shoved if nobody (including ourselves) is interacting with the lever
        if (currentActor != Actor.None)
            return;

        StartCoroutine(PerformLeverAction(isOwner, true));

        if (isOwner)
            channel.SendPacket([2]);
    }

    public void StartInteracting(Transform target, Actor actor)
    {
        currentActor = actor;
        animator.enabled = false;
        rotateTo = target;
        
        if (actor == Actor.Self)
            channel.SendPacket([0]);
    }

    public void StopInteracting()
    {
        try
        {
            if (shouldTrigger == TriggerDirection.LandShip && lever.playersManager.inShipPhase || shouldTrigger == TriggerDirection.DepartShip && !lever.playersManager.inShipPhase)
            {
                StartCoroutine(PerformLeverAction(currentActor == Actor.Self));
                return;
            }

            animator.enabled = true;
        }
        finally
        {
            if (currentActor == Actor.Self)
                channel.SendPacket([1]);
            
            // Always reset at the end
            rotateTo = null;
            shouldTrigger = TriggerDirection.None;
            currentActor = Actor.None;
        }
    }

    private IEnumerator PerformLeverAction(bool isLocal, bool isShove = false)
    {
        if (isShove)
            lever.leverAnimatorObject.SetBool(shoveLever, true);

        if (isLocal)
            lever.LeverAnimation();

        yield return new WaitForSeconds(1.67f);

        animator.enabled = true;

        if (isLocal)
            lever.PullLever();

        lever.leverAnimatorObject.SetBool(shoveLever, false);
    }

    private void OnOtherInteractWithLever(ushort other, BinaryReader reader)
    {
        var interaction = reader.ReadByte();

        if (!NetworkSystem.Instance.TryGetPlayer(other, out var player))
            return;

        switch (interaction)
        {
            case 0 when currentActor == Actor.None:
                StartInteracting(player.Bones.RightHand, Actor.Other);
                break;
            case 1 when currentActor == Actor.Other:
                StopInteracting();
                break;
            case 2:
                ShoveLever(false);
                break;
        }
    }

    public static void Create()
    {
        var startMatch = FindObjectOfType<StartMatchLever>();
        startMatch.leverAnimatorObject.gameObject.AddComponent<ShipLever>();
        startMatch.leverAnimatorObject.runtimeAnimatorController = AssetManager.IntroLeverAnimator;
        startMatch.leverAnimatorObject.GetComponent<PlayAudioAnimationEvent>().audioClip3 = AssetManager.LeverShove;

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
