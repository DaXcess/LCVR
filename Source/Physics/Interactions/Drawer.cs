using HarmonyLib;
using LCVR.Assets;
using LCVR.Patches;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LCVR.Physics.Interactions;

[LCVRPatch]
[HarmonyPatch]
internal static class GenericDrawersPatches
{
    [HarmonyPatch(typeof(NetworkObject), nameof(NetworkObject.Awake))]
    [HarmonyPostfix]
    private static void OnNetworkObjectCreate(NetworkObject __instance)
    {
        if (Plugin.Config.DisableDrawerInteraction.Value)
            return;

        if (__instance.name != "BathroomDrawers(Clone)" && __instance.name != "BedroomDrawers(Clone)" &&
            __instance.name != "BedroomDrawersB(Clone)" && __instance.name != "GreenhouseInteractables(Clone)" &&
            __instance.name != "GridCabinetContainer(Clone)")
            return;
        
        __instance.GetComponentsInChildren<InteractTrigger>().Do(component =>
        {
            if (component.touchTrigger)
                return;
            
            component.name = "DrawerInteractable";

            Object.Instantiate(AssetManager.Interactable, component.transform).AddComponent<GenericTrigger>();
        });
    }
}

[LCVRPatch]
[HarmonyPatch]
internal static class FancyDresserPatches
{
    [HarmonyPatch(typeof(NetworkObject), nameof(NetworkObject.Awake))]
    [HarmonyPostfix]
    private static void OnNetworkObjectCreate(NetworkObject __instance)
    {
        if (Plugin.Config.DisableDrawerInteraction.Value)
            return;

        if (__instance.name != "FancyDresserContainer(Clone)")
            return;

        (Vector3, Vector3)[] offsets =
        [
            (new Vector3(0.85f, 0.06f, -0.014f), new Vector3(0.12f, 0.06f, 0.26f)),
            (new Vector3(0.85f, 0.06f, -0.014f), new Vector3(0.12f, 0.06f, 0.26f)),
            (new Vector3(2.473f, 0.321f, -2.75f), new Vector3(0.3f, 0.53f, 1)),
            (new Vector3(2.473f, 0.321f, -2.75f), new Vector3(0.3f, 0.53f, 1)),
        ];

        var triggers = __instance.GetComponentsInChildren<InteractTrigger>();

        for (var i = 0; i < triggers.Length; i++)
        {
            var (position, scale) = offsets[i];
            var interactableObject = Object.Instantiate(AssetManager.Interactable, triggers[i].transform);
            interactableObject.transform.localPosition = position;
            interactableObject.transform.localScale = scale;
            interactableObject.AddComponent<GenericTrigger>();

            triggers[i].gameObject.name = "DrawerInteractable";
        }
    }
}

[LCVRPatch]
[HarmonyPatch]
internal static class FridgePatches
{
    [HarmonyPatch(typeof(NetworkObject), nameof(NetworkObject.Awake))]
    [HarmonyPostfix]
    private static void OnNetworkObjectCreate(NetworkObject __instance)
    {
        if (Plugin.Config.DisableDrawerInteraction.Value)
            return;

        if (__instance.name != "FridgeContainer(Clone)")
            return;

        foreach (var trigger in __instance.transform.Find("FridgeBody").GetComponentsInChildren<InteractTrigger>())
        {
            Object.Instantiate(AssetManager.Interactable, trigger.transform).AddComponent<GenericTrigger>();
            trigger.gameObject.name = "DrawerInteractable";
        }
    }
}

[LCVRPatch]
[HarmonyPatch]
internal static class StorageClosetPatches
{
    [HarmonyPatch(typeof(NetworkObject), nameof(NetworkObject.Awake))]
    [HarmonyPostfix]
    private static void OnNetworkObjectCreate(NetworkObject __instance)
    {
        if (Plugin.Config.DisableDrawerInteraction.Value)
            return;

        if (__instance.name != "StorageCloset")
            return;

        var position = new Vector3(-0.4017f, -0.392f, 0.052f);
        var scale = new Vector3(0.06f, 0.8f, 0.1f);

        Transform[] triggers = [__instance.transform.Find("Cube.000/Cube"), __instance.transform.Find("Cube.002/Cube")];

        foreach (var trigger in triggers)
        {
            var interactableObject = Object.Instantiate(AssetManager.Interactable, trigger.transform);
            interactableObject.transform.localPosition = position;
            interactableObject.transform.localScale = scale;
            interactableObject.AddComponent<GenericTrigger>();

            trigger.gameObject.name = "DrawerInteractable";
        }
    }
}