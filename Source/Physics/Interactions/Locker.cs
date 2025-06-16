using HarmonyLib;
using LCVR.Assets;
using LCVR.Patches;
using Unity.Netcode;
using UnityEngine;

namespace LCVR.Physics.Interactions;

[LCVRPatch]
[HarmonyPatch]
internal static class LockerPatches
{
    [HarmonyPatch(typeof(InteractTrigger), nameof(InteractTrigger.Start))]
    [HarmonyPostfix]
    private static void OnTriggerCreate(InteractTrigger __instance)
    {
        if (Plugin.Config.DisableDrawerInteraction.Value)
            return;
        
        if (__instance.GetComponentInParent<NetworkObject>() is not { } networkObject)
            return;

        if (networkObject.name != "StorageShelfContainer(Clone)")
            return;

        var interactableObject = Object.Instantiate(AssetManager.Interactable, __instance.transform);
        interactableObject.transform.localPosition = new Vector3(0.38f, 0.4014f, 0.0217f);
        interactableObject.transform.localEulerAngles = Vector3.zero;
        interactableObject.transform.localScale = new Vector3(1, 0.06f, 0.1f);
        interactableObject.AddComponent<GenericTrigger>();
    }
}