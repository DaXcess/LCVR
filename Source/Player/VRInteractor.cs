using LCVR.Assets;
using LCVR.Input;
using LCVR.Physics;
using System.Collections.Generic;
using System.Linq;
using LCVR.Managers;
using UnityEngine;
using UnityEngine.XR;

namespace LCVR.Player;

[DefaultExecutionOrder(-100)]
public class VRInteractor : MonoBehaviour
{
    private const int INTERACTABLE_OBJECT_MASK = 1 << 11;

    private static readonly Offset RightHandDefaultOffset =
        new(new Vector3(-0.003f, 0.18f, -0.01f), new Vector3(0.14f, 0.1f, 0.04f), Quaternion.Euler(0, 10, 0));

    private static readonly Offset RightHandFingerOffset = new(new Vector3(0.05f, 0.18f, -0.02f),
        new Vector3(0.04f, 0.1f, 0.04f), Quaternion.Euler(0, 10, 0));

    private static readonly Offset RightHandFistOffset =
        new(new Vector3(-0.02f, 0.09f, 0), new Vector3(0.12f, 0.1f, 0.07f), Quaternion.identity);

    private static readonly Offset LeftHandDefaultOffset = new(new Vector3(0.003f, 0.19f, -0.01f),
        new Vector3(0.14f, 0.1f, 0.04f), Quaternion.Euler(0, 350, 0));

    private static readonly Offset LeftHandFingerOffset = new(new Vector3(-0.05f, 0.19f, -0.02f),
        new Vector3(0.04f, 0.1f, 0.04f), Quaternion.Euler(0, 350, 0));

    private static readonly Offset LeftHandFistOffset =
        new(new Vector3(0.02f, 0.09f, 0), new Vector3(0.12f, 0.1f, 0.07f), Quaternion.identity);

    private readonly Collider[] colliderPool = new Collider[8];

    private Transform debugCube;
    private Offset defaultOffset;
    private Offset fingerOffset;
    private Offset fistOffset;

    internal bool isHeld;

    public bool IsRightHand { get; private set; }
    public VRFingerCurler FingerCurler { get; private set; }
    public Transform TrackedController { get; private set; }

    private void Start()
    {
        switch (gameObject.name)
        {
            case "hand.R":
                defaultOffset = RightHandDefaultOffset;
                fingerOffset = RightHandFingerOffset;
                fistOffset = RightHandFistOffset;

                IsRightHand = true;
                FingerCurler = VRSession.Instance.LocalPlayer.RightFingerCurler;
                TrackedController = VRSession.Instance.LocalPlayer.RightHandVRTarget;
                break;
            case "hand.L":
                defaultOffset = LeftHandDefaultOffset;
                fingerOffset = LeftHandFingerOffset;
                fistOffset = LeftHandFistOffset;

                IsRightHand = false;
                FingerCurler = VRSession.Instance.LocalPlayer.LeftFingerCurler;
                TrackedController = VRSession.Instance.LocalPlayer.LeftHandVRTarget;
                break;
            default:
                throw new System.Exception($"Attached to unknown object: {gameObject.name}");
        }

        debugCube = Instantiate(AssetManager.Interactable, transform).transform;
        debugCube.localScale = Vector3.zero;
    }

    private void Update()
    {
        var index = FingerCurler.indexFinger.curl > 0.75f;
        var grip = FingerCurler.middleFinger.curl > 0.75f;

        var offset = (index, grip) switch
        {
            (true, true) => fistOffset,
            (false, true) => fingerOffset,
            _ => defaultOffset
        };

        debugCube.localPosition = offset.position;
        debugCube.localRotation = offset.rotation;
        debugCube.localScale = offset.scale;

        var center = transform.TransformPoint(offset.OverlapPosition);
        var count = UnityEngine.Physics.OverlapBoxNonAlloc(center, offset.OverlapScale, colliderPool,
            transform.rotation * offset.OverlapRotation, INTERACTABLE_OBJECT_MASK);
        var interactables = colliderPool[..count].OrderBy(c => (center - c.transform.position).sqrMagnitude)
            .Select(c => c.GetComponent<VRInteractable>())
            .Where(i => i != null);

        VRSession.Instance.InteractionManager.ReportInteractables(this, interactables.ToArray());
    }

    /// <summary>
    /// Reset LocalItemHolder transforms after the animator modifies them but before vanilla scripts access them
    /// </summary>
    private void LateUpdate()
    {
        var bones = VRSession.Instance.LocalPlayer.Bones;
        
        bones.LocalItemHolder.localPosition = new Vector3(-0.002f, 0.036f, -0.042f);
        bones.LocalItemHolder.localEulerAngles = new Vector3(356.3837f, 357.6979f, 0.1453f);
    }

    private void OnDisable()
    {
        // Force reset the interaction state
        VRSession.Instance.InteractionManager.ResetState();
    }

    public bool IsPressed()
    {
        var action = Actions.Instance[$"Interact{(IsRightHand ? "" : "Left")}"];

        return enabled && action is not null && action.IsPressed();
    }

    public void SnapTo(Transform targetTransform, Vector3? positionOffset = null, Vector3? rotationOffset = null)
    {
        var player = VRSession.Instance.LocalPlayer;
        
        var localTracker = IsRightHand
            ? player.RigTrackerLocal.rightHand
            : player.RigTrackerLocal.leftHand;

        var tracker = IsRightHand ? player.RigTracker.rightHand : player.RigTracker.leftHand;
        
        if (targetTransform == null)
        {
            var source = IsRightHand ? player.RightHandVRTarget : player.LeftHandVRTarget;
            
            localTracker.srcTransform = source;
            localTracker.positionOffset = Vector3.zero;
            localTracker.rotationOffset = Vector3.zero;
            
            tracker.srcTransform = source;
            tracker.positionOffset = Vector3.zero;
            tracker.rotationOffset = Vector3.zero;
        }
        else
        {
            localTracker.srcTransform = targetTransform;
            localTracker.positionOffset = positionOffset ?? Vector3.zero;
            localTracker.rotationOffset = rotationOffset ?? Vector3.zero;
            
            tracker.srcTransform = targetTransform;
            tracker.positionOffset = positionOffset ?? Vector3.zero;
            tracker.rotationOffset = rotationOffset ?? Vector3.zero;
        }
    }

    public void Vibrate(float duration, float amplitude)
    {
        VRSession.VibrateController(IsRightHand ? XRNode.RightHand : XRNode.LeftHand, duration, amplitude);
    }

    private readonly struct Offset(Vector3 position, Vector3 scale, Quaternion rotation)
    {
        public readonly Vector3 position = position;
        public readonly Vector3 scale = scale;
        public readonly Quaternion rotation = rotation;

        public Vector3 OverlapPosition => position;
        public Vector3 OverlapScale => scale / 2;
        public Quaternion OverlapRotation => rotation;
    }
}

/// <summary>
/// Manages VR interactions between interactors (hands) and interactables (objects).
/// Supports multiple interactors per interactable, allowing both hands to interact with the same object simultaneously.
/// </summary>
public class InteractionManager
{
    private readonly Dictionary<VRInteractable, Dictionary<VRInteractor, InteractableState>> interactableStates = [];

    public void ReportInteractables(VRInteractor interactor, VRInteractable[] interactables)
    {
        foreach (var interactable in interactables)
        {
            // Initialize state dictionary for this interactable if needed
            if (!interactableStates.ContainsKey(interactable))
                interactableStates[interactable] = [];

            var states = interactableStates[interactable];

            // Check if this hand is allowed to interact
            if (interactor.IsRightHand && !interactable.Flags.HasFlag(InteractableFlags.RightHand))
                continue;

            if (!interactor.IsRightHand && !interactable.Flags.HasFlag(InteractableFlags.LeftHand))
                continue;

            if (interactor.isHeld && interactable.Flags.HasFlag(InteractableFlags.NotWhileHeld))
                continue;

            // Add this interactor if not already tracking
            if (!states.ContainsKey(interactor))
            {
                states[interactor] = new InteractableState(false);
                interactable.OnColliderEnter(interactor);
            }

            var state = states[interactor];

            // Handle button press
            if (!state.isHeld && !interactor.isHeld && interactor.IsPressed())
            {
                var acknowledged = interactable.OnButtonPress(interactor);
                state.isHeld = interactor.isHeld = acknowledged;
            }
            // Handle button release
            else if (state.isHeld && !interactor.IsPressed())
            {
                interactable.OnButtonRelease(interactor);
                state.isHeld = interactor.isHeld = false;
            }
        }

        // Clean up interactors that left the collision zone
        var interactablesToRemove = new List<VRInteractable>();
        
        foreach (var (interactable, states) in interactableStates)
        {
            if (!states.ContainsKey(interactor))
                continue;

            if (interactables.Contains(interactable))
                continue;

            var state = states[interactor];

            // Release if still held
            if (state.isHeld)
            {
                if (interactor.IsPressed())
                    continue;
                
                interactable.OnButtonRelease(interactor);
                interactor.isHeld = false;
            }

            interactable.OnColliderExit(interactor);
            states.Remove(interactor);

            // Clean up empty state dictionaries
            if (states.Count == 0)
                interactablesToRemove.Add(interactable);
        }

        foreach (var interactable in interactablesToRemove)
            interactableStates.Remove(interactable);
    }

    public void ResetState()
    {
        foreach (var (interactable, states) in interactableStates)
        {
            foreach (var (interactor, state) in states)
            {
                if (state.isHeld)
                    interactable.OnButtonRelease(interactor);
                
                interactable.OnColliderExit(interactor);
            }
        }
        
        interactableStates.Clear();
    }

    private class InteractableState(bool isHeld)
    {
        public bool isHeld = isHeld;
    }
}