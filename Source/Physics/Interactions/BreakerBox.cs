using HarmonyLib;
using LCVR.Assets;
using LCVR.Patches;
using LCVR.Player;
using UnityEngine;

namespace LCVR.Physics.Interactions;

internal class BreakerBoxSwitch : MonoBehaviour, VRInteractable
{
    private InteractTrigger trigger;
    internal BreakerBoxDoor door;
    private float lastInteraction;

    public InteractableFlags Flags => InteractableFlags.BothHands;

    void Awake()
    {
        trigger = GetComponentInParent<InteractTrigger>();
    }

    public void OnColliderEnter(VRInteractor interactor)
    {
        if (!door.IsOpen || !interactor.FingerCurler.IsPointer || !trigger.interactable || Time.realtimeSinceStartup - lastInteraction < 0.25f)
            return;

        lastInteraction = Time.realtimeSinceStartup;
        trigger.Interact(VRSession.Instance.LocalPlayer.transform);
    }

    public void OnColliderExit(VRInteractor _) { }
    public bool OnButtonPress(VRInteractor _) { return false; }
    public void OnButtonRelease(VRInteractor _) { }
}

internal class BreakerBoxDoor : MonoBehaviour, VRInteractable
{
    private AnimatedObjectTrigger animatedTrigger;
    private InteractTrigger trigger;
    private float lastInteraction;

    public InteractableFlags Flags => InteractableFlags.BothHands;
    public bool IsOpen => animatedTrigger.boolValue && Time.realtimeSinceStartup - lastInteraction > 0.5f;

    void Awake()
    {
        animatedTrigger = GetComponentInParent<AnimatedObjectTrigger>();
        trigger = GetComponentInParent<InteractTrigger>();
    }

    public void OnColliderEnter(VRInteractor interactor)
    {
        if (!trigger.interactable || Time.realtimeSinceStartup - lastInteraction < 1f)
            return;

        lastInteraction = Time.realtimeSinceStartup;
        trigger.Interact(VRSession.Instance.LocalPlayer.transform);
    }

    public bool OnButtonPress(VRInteractor _) { return false; }
    public void OnButtonRelease(VRInteractor _) { }
    public void OnColliderExit(VRInteractor _) { }
}

[LCVRPatch]
[HarmonyPatch]
internal static class BreakerBoxPatches
{
    [HarmonyPatch(typeof(BreakerBox), nameof(BreakerBox.Start))]
    [HarmonyPostfix]
    private static void OnCreateBreakerBox(BreakerBox __instance)
    {
        if (Plugin.Config.DisableBreakerBoxInteraction.Value)
            return;

        var door = __instance.transform.Find("Mesh/PowerBoxDoor");
        var doorInteractableObject = Object.Instantiate(AssetManager.Interactable, door);
        doorInteractableObject.transform.localPosition = new Vector3(-0.8f, 0, -0.7f);
        doorInteractableObject.transform.localScale = new Vector3(1.5f, 0.2f, 2.1f);

        var doorInteractable = doorInteractableObject.AddComponent<BreakerBoxDoor>();

        for (var i = 1; i <= 5; i++)
        {
            var @switch = __instance.transform.Find($"Mesh/BreakerSwitch{i}");
            var switchInteractableObject = Object.Instantiate(AssetManager.Interactable, @switch);
            switchInteractableObject.transform.localEulerAngles = new Vector3(0, 45, 0);
            switchInteractableObject.transform.localScale = Vector3.one * 0.1f;

            var switchInteractable = switchInteractableObject.AddComponent<BreakerBoxSwitch>();
            switchInteractable.door = doorInteractable;
        }
    }
}
