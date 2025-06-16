using HarmonyLib;
using LCVR.Assets;
using LCVR.Managers;
using LCVR.Patches;
using LCVR.Player;
using UnityEngine;

namespace LCVR.Physics.Interactions;

internal class ElevatorButton : MonoBehaviour, VRInteractable
{
    private InteractTrigger trigger;
    private float lastInteractTime;

    private bool CanInteract => Time.realtimeSinceStartup - lastInteractTime > 0.25f;

    public InteractableFlags Flags => InteractableFlags.BothHands;

    private void Awake()
    {
        trigger = GetComponentInParent<InteractTrigger>();
        trigger.gameObject.name = "ElevatorButtonTrigger";
    }

    public void OnColliderEnter(VRInteractor interactor)
    {
        if (!CanInteract)
            return;

        lastInteractTime = Time.realtimeSinceStartup;
        trigger.onInteract?.Invoke(VRSession.Instance.LocalPlayer.PlayerController);
        interactor.Vibrate(0.1f, 0.1f);
    }

    public void OnColliderExit(VRInteractor _) { }
    public bool OnButtonPress(VRInteractor _) { return false; }
    public void OnButtonRelease(VRInteractor _) { }
}

[LCVRPatch]
[HarmonyPatch]
internal static class MonitorButtonPatches
{
    [HarmonyPatch(typeof(MineshaftElevatorController), nameof(MineshaftElevatorController.OnEnable))]
    [HarmonyPostfix]
    private static void OnButtonCreate(MineshaftElevatorController __instance)
    {
        if (Plugin.Config.DisableElevatorButtonInteraction.Value)
            return;

        var buttons = new[]
        {
            __instance.transform.Find("AnimContainer/ElevatorButtonTrigger/Cube"),
            __instance.transform.Find("TopElevatorPanel/ElevatorButtonTrigger (1)/Cube"),
            __instance.transform.Find("BottomElevatorPanel/ElevatorButtonTrigger (1)/Cube")
        };

        foreach (var button in buttons)
        {
            var interactableObject = Object.Instantiate(AssetManager.Interactable, button);

            interactableObject.transform.localPosition = new Vector3(-0.3f, 0.14f, 0.09f);
            interactableObject.transform.localScale = Vector3.one * 0.4f;
            interactableObject.AddComponent<ElevatorButton>();
        }
    }
}