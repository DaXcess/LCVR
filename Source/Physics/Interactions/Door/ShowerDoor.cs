using HarmonyLib;
using LCVR.Assets;
using LCVR.Patches;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LCVR.Physics.Interactions.Door;

[LCVRPatch]
[HarmonyPatch]
internal static class ShowerDoorPatches
{
    [HarmonyPatch(typeof(InteractTrigger), nameof(InteractTrigger.Start))]
    [HarmonyPostfix]
    private static void OnInteractStart(InteractTrigger __instance)
    {
        if (Plugin.Config.DisableDoorInteraction.Value)
            return;
        
        if (__instance.GetComponentInParent<NetworkObject>() is not { } networkObject)
            return;

        if (networkObject.name != "BathroomShowerDoor(Clone)")
            return;

        // Make sure default ray based interaction no longer works for this door
        __instance.gameObject.name = "DoorInteractable";
        
        var interactableObject = Object.Instantiate(AssetManager.Interactable, __instance.transform);
        interactableObject.transform.localPosition = new Vector3(-0.408f, 0.05f, -0.0483f);
        interactableObject.transform.localEulerAngles = Vector3.zero;
        interactableObject.transform.localScale = new Vector3(0.05f, 0.7f, 0.1f);
        interactableObject.AddComponent<GenericTrigger>();
    }
}