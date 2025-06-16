using HarmonyLib;
using LCVR.Assets;
using LCVR.Patches;
using Unity.Netcode;
using UnityEngine;

namespace LCVR.Physics.Interactions.Door;

[LCVRPatch]
[HarmonyPatch]
internal static class GlassDoubleDoorsPatches
{
    [HarmonyPatch(typeof(DoorLock), nameof(DoorLock.Awake))]
    [HarmonyPostfix]
    private static void InitializeDoorInteractor(DoorLock __instance)
    {
        if (Plugin.Config.DisableDoorInteraction.Value)
            return;

        if (__instance.GetComponentInParent<NetworkObject>() is not { } networkObject)
            return;

        if (networkObject.name != "GlassDoubleDoorsMapModel(Clone)")
            return;
        
        // Make sure default ray based interaction no longer works for this door
        __instance.gameObject.name = "DoorInteractable";

        foreach (var child in __instance.transform.parent.GetComponentsInChildren<TriggerPointToDoor>())
        {
            var interactableObject = Object.Instantiate(AssetManager.Interactable, child.transform);
            var interactable = interactableObject.AddComponent<GenericDoor>();
            
            interactableObject.transform.localPosition =
                child.name == "DoorLeftTrigger" ? new Vector3(0, 0.3245f, -0.013f) : new Vector3(-0.05f, -0.325f, -0.013f);
            interactableObject.transform.localEulerAngles = Vector3.zero;
            interactableObject.transform.localScale = new Vector3(1.53f, 0.13f, 0.02f);

            interactable.door = __instance;
            interactable.wasOpened = __instance.isDoorOpened;
            
            child.gameObject.name = "DoorInteractable";
        }
    }
}