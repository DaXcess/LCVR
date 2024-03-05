using LCVR.Assets;
using LCVR.Input;
using LCVR.Physics;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LCVR.Player;

public class VRInteractor : MonoBehaviour
{
    private const int interactableObjectMask = 1 << 11;

    private static readonly Offset rightHandDefaultOffset = new(new(-0.003f, 0.18f, -0.01f), new(0.14f, 0.1f, 0.04f), Quaternion.Euler(0, 10, 0));
    private static readonly Offset rightHandFingerOffset = new(new(0.05f, 0.18f, -0.02f), new(0.04f, 0.1f, 0.04f), Quaternion.Euler(0, 10, 0));
    private static readonly Offset rightHandFistOffset = new(new(-0.02f, 0.09f, 0), new(0.12f, 0.1f, 0.07f), Quaternion.identity);
    
    private static readonly Offset leftHandDefaultOffset = new(new(0.003f, 0.19f, -0.01f), new(0.14f, 0.1f, 0.04f), Quaternion.Euler(0, 350, 0));
    private static readonly Offset leftHandFingerOffset = new(new(-0.05f, 0.19f, -0.02f), new(0.04f, 0.1f, 0.04f), Quaternion.Euler(0, 350, 0));
    private static readonly Offset leftHandFistOffset = new(new(0.02f, 0.09f, 0), new(0.12f, 0.1f, 0.07f), Quaternion.identity);

    private Transform debugCube;
    private Offset defaultOffset;
    private Offset fingerOffset;
    private Offset fistOffset;

    public bool IsRightHand { get; private set; }
    public VRFingerCurler FingerCurler { get; private set; }

    void Start()
    {
        if (gameObject.name == "hand.R")
        {
            defaultOffset = rightHandDefaultOffset;
            fingerOffset = rightHandFingerOffset;
            fistOffset = rightHandFistOffset;
            
            IsRightHand = true;
            FingerCurler = VRSession.Instance.LocalPlayer.RightFingerCurler;
        }
        else if (gameObject.name == "hand.L")
        {
            defaultOffset = leftHandDefaultOffset;
            fingerOffset = leftHandFingerOffset;
            fistOffset = leftHandFistOffset;

            IsRightHand = false;
            FingerCurler = VRSession.Instance.LocalPlayer.LeftFingerCurler;
        }
        else
            throw new System.Exception($"Attached to unknown object: {gameObject.name}"); 

        debugCube = Instantiate(AssetManager.interactable, transform).transform;
        debugCube.localScale = Vector3.zero;
    }

    void Update()
    {
        var index = FingerCurler.indexFinger.curl > 0.75f;
        var grip = FingerCurler.middleFinger.curl > 0.75f;

        var offset = (index, grip) switch
        {
            (true, true) => fistOffset,
            (false, true) => fingerOffset,
            _ => defaultOffset
        };

        debugCube.transform.localPosition = offset.position;
        debugCube.transform.localRotation = offset.rotation;
        debugCube.transform.localScale = offset.scale;

        var center = transform.TransformPoint(offset.OverlapPosition);
        var objects = UnityEngine.Physics.OverlapBox(center, offset.OverlapScale, transform.rotation * offset.OverlapRotation, interactableObjectMask);
        var interactables = objects.Select(o => o.GetComponent<VRInteractable>()).Where(o => o != null);

        VRSession.Instance.InteractionManager.ReportInteractables(this, interactables);
    }

    public bool IsPressed()
    {
        return Actions.Instance[$"Controls/Interact{(IsRightHand ? "" : "Left")}"].IsPressed();
    }

    private struct Offset(Vector3 position, Vector3 scale, Quaternion rotation)
    {
        public Vector3 position = position;
        public Vector3 scale = scale;
        public Quaternion rotation = rotation;

        public readonly Vector3 OverlapPosition => position;
        public readonly Vector3 OverlapScale => scale / 2;
        public readonly Quaternion OverlapRotation => rotation;
    }
}

public class InteractionManager
{
    private readonly Dictionary<VRInteractable, InteractableState> interactableState = [];

    public void ReportInteractables(VRInteractor interactor, IEnumerable<VRInteractable> interactables)
    {
        foreach (var interactable in interactables)
        {
            if (!interactableState.ContainsKey(interactable))
            {
                // Check if this hand is allowed to interact with this object
                if (interactor.IsRightHand && !interactable.Flags.HasFlag(InteractableFlags.RightHand))
                    continue;

                if (!interactor.IsRightHand && !interactable.Flags.HasFlag(InteractableFlags.LeftHand))
                    continue;

                interactableState.Add(interactable, new InteractableState(interactor, false));
                interactable.OnColliderEnter(interactor);
            }

            // Ignore events from hand if other hand is already interacting
            if (interactableState[interactable].interactor != interactor)
                continue;

            if (!interactableState[interactable].isHeld && interactor.IsPressed())
            {
                bool acknowledged = interactable.OnButtonPress(interactor);
                interactableState[interactable].isHeld = acknowledged;
            }
            else if (interactableState[interactable].isHeld && !interactor.IsPressed())
            {
                interactable.OnButtonRelease(interactor);
                interactableState[interactable].isHeld = false;
            }
        }

        foreach (var interactable in interactableState.Keys)
        {
            // Ignore if this state is being managed by another hand
            if (interactableState[interactable].interactor != interactor)
                continue;

            if (!interactables.Contains(interactable))
            {
                // Ignore if button is still being held down
                if (interactableState[interactable].isHeld)
                    if (interactor.IsPressed())
                        continue;
                    else
                        interactable.OnButtonRelease(interactor);

                interactableState.Remove(interactable);
                interactable.OnColliderExit(interactor);

                // Break to not make C# shit itself, if more need to be removed it'll happen next frame
                break;
            }
        }
    }

    private class InteractableState(VRInteractor interactor, bool isHeld)
    {
        public VRInteractor interactor = interactor;
        public bool isHeld = isHeld;
    }
}
