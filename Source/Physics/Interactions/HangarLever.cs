using HarmonyLib;
using LCVR.Assets;
using LCVR.Patches;
using LCVR.Player;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LCVR.Physics.Interactions;

public class HangarLever : MonoBehaviour, VRInteractable
{
    private InteractTrigger trigger;
    private AnimatedObjectTrigger animTrigger;
    
    private bool interacting;
    private Transform lookAtTransform;

    private float lockedRotation;
    private float lockedRotationTime;

    internal bool inverse;
    
    public InteractableFlags Flags => InteractableFlags.BothHands;

    private void Awake()
    {
        trigger = GetComponentInParent<InteractTrigger>();
        animTrigger = GetComponentInParent<AnimatedObjectTrigger>();

        transform.parent.GetComponentInParent<BoxCollider>().enabled = false;
    }
    
    public void OnColliderEnter(VRInteractor interactor)
    {
    }

    public void OnColliderExit(VRInteractor interactor)
    {
    }

    public bool OnButtonPress(VRInteractor interactor)
    {
        if (!trigger.interactable)
            return false;

        interacting = true;
        lookAtTransform = interactor.transform;
        
        interactor.FingerCurler.ForceFist(true);
        
        return true;
    }

    public void OnButtonRelease(VRInteractor interactor)
    {
        interactor.FingerCurler.ForceFist(false);

        if (!interacting)
            return;

        interacting = false;

        var rot = GetLookRotation();
        switch ((!inverse && animTrigger.boolValue) || (inverse && !animTrigger.boolValue))
        {
            case true when rot is > 160 and < 200:
                trigger.Interact(VRSession.Instance.LocalPlayer.transform);
                lockedRotation = 180;
                lockedRotationTime = Time.realtimeSinceStartup + 0.5f;
                
                break;
            case false when rot < 20:
                trigger.Interact(VRSession.Instance.LocalPlayer.transform);
                lockedRotation = 0;
                lockedRotationTime = Time.realtimeSinceStartup + 0.5f;
                
                break;
        }
    }

    private void LateUpdate()
    {
        if (lockedRotationTime > Time.realtimeSinceStartup)
        {
            transform.parent.localEulerAngles = new Vector3(0, lockedRotation, 0);
            return;
        }
        
        if (!interacting)
            return;

        transform.parent.localEulerAngles = new Vector3(0, GetLookRotation(), 0);
    }

    private float GetLookRotation()
    {
        // I know, I'm supposed to calculate this myself but idk how and internet is not helping
        var tf = transform.parent;
        
        var rotation = tf.rotation;
        tf.LookAt(lookAtTransform.position);
        
        var lookRotation = tf.localEulerAngles.y;
        tf.rotation = rotation;

        // Prevent specific issue with the lever jumping down
        if (lookRotation > 270)
            return 0;
        
        return Mathf.Clamp(lookRotation, 0, 180);
    }
}

[LCVRPatch]
[HarmonyPatch]
internal static class LeverSwitchPatches
{
    [HarmonyPatch(typeof(InteractTrigger), nameof(InteractTrigger.Start))]
    [HarmonyPostfix]
    private static void OnInteractTriggerStart(InteractTrigger __instance)
    {
        var go = __instance.gameObject;
        if (go.name is not "LeverSwitchHandle" and not "MagnetLever")
            return;
        
        var isMagnet = go.name is "MagnetLever";
        go.name = "LeverSwitchInteractable";
        
        var interactableObject = Object.Instantiate(AssetManager.Interactable,
            isMagnet ? __instance.transform.Find("LeverSwitchHandle") : __instance.transform);
        
        interactableObject.transform.localPosition = new Vector3(0.0044f, -0.0513f, 0.2529f);
        interactableObject.transform.localScale = new Vector3(0.0553f, 0.1696f, 0.0342f);
        
        var lever = interactableObject.AddComponent<HangarLever>();
        lever.inverse = isMagnet;
    }
}