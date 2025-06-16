using LCVR.Managers;
using LCVR.Player;
using UnityEngine;

namespace LCVR.Physics.Interactions;

public class GenericTrigger : MonoBehaviour, VRInteractable
{
    public InteractableFlags Flags => InteractableFlags.BothHands;

    private InteractTrigger trigger;

    private void Awake()
    {
        trigger = GetComponentInParent<InteractTrigger>();
    }

    public bool OnButtonPress(VRInteractor interactor)
    {
        trigger.Interact(VRSession.Instance.LocalPlayer.transform);

        return true;
    }

    public void OnColliderEnter(VRInteractor interactor)
    {
        interactor.Vibrate(0.1f, 0.1f);
    }

    public void OnColliderExit(VRInteractor interactor)
    {
    }

    public void OnButtonRelease(VRInteractor interactor)
    {
    }
}
