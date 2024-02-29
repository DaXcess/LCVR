﻿using System;
using System.Collections.Generic;
using HarmonyLib;
using LCVR.Assets;
using LCVR.Patches;
using LCVR.Player;
using Unity.Netcode;
using UnityEngine;

namespace LCVR.Physics.Interactions;

internal class Door : MonoBehaviour, VRInteractable
{
    internal static Dictionary<string, (Vector3, Vector3, Vector3)> registeredDoorHandles = new()
    {
        ["SteelDoorMapModel"] = (new(-0.29f, 0.2336f, -0.025f), Vector3.zero, new(1, 0.15f, 0.025f)),
        ["FancyDoorMapModel"] = (new(-0.29f, 0.2836f, -0.055f), Vector3.zero, new(1, 0.09f, 0.155f))
    };

    internal DoorLock door;
    private float lastInteraction;

    public InteractableFlags Flags => InteractableFlags.BothHands;

    public bool OnButtonPress(VRInteractor interactor)
    {
        if (door.isLocked)
        {
            var item = VRSession.Instance.LocalPlayer.PlayerController.currentlyHeldObjectServer;
            var unlocking = !door.isPickingLock && interactor.IsRightHand && item?.itemProperties.itemId == 14;
            var picking = !door.isPickingLock && interactor.IsRightHand && item?.itemProperties.itemId == 8;

            if (unlocking)
            {
                door.UnlockDoorSyncWithServer();
                item.playerHeldBy.DespawnHeldObject();

                // Why isn't this done by DespawnHeldObject?
                item.playerHeldBy.currentlyHeldObjectServer = null;
            }
            else if (picking && item is LockPicker lockpicker)
            {
                var position = lockpicker.GetLockPickerDoorPosition(door);

                lockpicker.playerHeldBy.DiscardHeldObject(true, door.NetworkObject, position, true);
                lockpicker.PlaceLockPickerServerRpc(door.NetworkObject, lockpicker.placeOnLockPicker1);
                lockpicker.PlaceOnDoor(door, lockpicker.placeOnLockPicker1);
            }
            else
                door.doorLockSFX.PlayOneShot(AssetManager.doorLocked);

            return true;
        }

        if (Time.realtimeSinceStartup - lastInteraction < 0.5f)
            return false;

        door.OpenOrCloseDoor(VRSession.Instance.LocalPlayer.PlayerController);
        lastInteraction = Time.realtimeSinceStartup;

        return true;
    }

    public void OnButtonRelease(VRInteractor _) { }
    public void OnColliderEnter(VRInteractor _) { }
    public void OnColliderExit(VRInteractor _) { }

    /// <summary>
    /// Registers a new VR interactable door
    /// </summary>
    /// <param name="networkObject">The network object for this door</param>
    /// <param name="position">The position offset for the door handle</param>
    /// <param name="rotation">The rotation offset for the door handle</param>
    /// <param name="scale">The scale for the door handle</param>
    public static void RegisterDoor(NetworkObject networkObject, Vector3? position = null, Vector3? rotation = null, Vector3? scale = null)
    {
        if (networkObject == null)
            throw new ArgumentNullException(nameof(networkObject));

        position ??= Vector3.zero;
        rotation ??= Vector3.zero;
        scale ??= Vector3.zero;

        RegisterDoor(networkObject.name, position, rotation, scale);
    }

    /// <summary>
    /// Registers a new VR interactable door
    /// </summary>
    /// <param name="networkObjectName">The name of the network object for this door</param>
    /// <param name="position">The position offset for the door handle</param>
    /// <param name="rotation">The rotation offset for the door handle</param>
    /// <param name="scale">The scale for the door handle</param>
    public static void RegisterDoor(string networkObjectName, Vector3? position = null, Vector3? rotation = null, Vector3? scale = null)
    {
        if (string.IsNullOrEmpty(networkObjectName))
            throw new ArgumentNullException(nameof(networkObjectName));

        position ??= Vector3.zero;
        rotation ??= Vector3.zero;
        scale ??= Vector3.zero;

        int idx = networkObjectName.LastIndexOf("(Clone)");

        if (idx > -1)
            networkObjectName = networkObjectName.Remove(idx, 7);

        registeredDoorHandles.Add(networkObjectName, (position.Value, rotation.Value, scale.Value));
    }
}

internal class LockPickerInteractable : MonoBehaviour, VRInteractable
{
    private LockPicker lockPicker;

    public InteractableFlags Flags => InteractableFlags.RightHand;

    void Awake()
    {
        lockPicker = GetComponentInParent<LockPicker>();
    }

    public bool OnButtonPress(VRInteractor interactor)
    {
        if (!lockPicker.isOnDoor || lockPicker.currentlyPickingDoor.maxTimeLeft - lockPicker.currentlyPickingDoor.lockPickTimeLeft < 1f)
            return true;

        VRSession.Instance.LocalPlayer.PrimaryController.GrabItem(lockPicker);

        return true;
    }

    public void OnButtonRelease(VRInteractor _) { }
    public void OnColliderEnter(VRInteractor _) { }
    public void OnColliderExit(VRInteractor _) { }
}

[LCVRPatch]
[HarmonyPatch]
internal static class DoorPatches
{
    [HarmonyPatch(typeof(DoorLock), "Awake")]
    [HarmonyPostfix]
    private static void InitializeDoorInteractor(DoorLock __instance)
    {
        if (Plugin.Config.DisableDoorInteraction.Value)
            return;

        if (!Door.registeredDoorHandles.TryGetValue(__instance.NetworkObject.name, out var offsets))
        {
            int idx = __instance.NetworkObject.name.LastIndexOf("(Clone)");

            if (idx < 0)
                return;

            var name = __instance.NetworkObject.name.Remove(idx, 7);

            if (!Door.registeredDoorHandles.TryGetValue(name, out offsets))
                return;
        }


        var (position, rotation, scale) = offsets;

        // Make sure default ray based interaction no longer works for this door
        __instance.gameObject.name = "DoorInteractable";

        var interactableObject = GameObject.Instantiate(AssetManager.interactable, __instance.transform);
        interactableObject.transform.localPosition = position;
        interactableObject.transform.localEulerAngles = rotation;
        interactableObject.transform.localScale = scale;

        var interactable = interactableObject.AddComponent<Door>();
        interactable.door = __instance;
    }
}

[LCVRPatch]
[HarmonyPatch]
internal static class LockerPickerPatches
{
    [HarmonyPatch(typeof(LockPicker), "Start")]
    [HarmonyPostfix]
    private static void InitializeLockPickerInteractor(LockPicker __instance)
    {
        if (Plugin.Config.DisableDoorInteraction.Value)
            return;

        // Do **not** yet disable the ray based interactor. Only disable when placed on door!
        __instance.gameObject.name = "LockPicker";

        var interactableObject = GameObject.Instantiate(AssetManager.interactable, __instance.transform);
        interactableObject.transform.localScale = new Vector3(2f, 4f, 2.5f);
        interactableObject.AddComponent<LockPickerInteractable>();
    }

    /// <summary>
    /// Change object name so that VRController no longer allows picking up the item when placed on a door
    /// </summary>
    [HarmonyPatch(typeof(LockPicker), nameof(LockPicker.PlaceOnDoor))]
    [HarmonyPostfix]
    private static void OnPlacedOnDoor(LockPicker __instance)
    {
        __instance.gameObject.name = "LockPickerInteractable";
    }


    /// <summary>
    /// Change object name so that VRController allows picking up the item when removed from a door
    /// </summary>
    [HarmonyPatch(typeof(LockPicker), "RetractClaws")]
    [HarmonyPostfix]
    private static void OnRemovedFromDoor(LockPicker __instance)
    {
        __instance.gameObject.name = "LockPicker";
    }

    /// <summary>
    /// Prevent placing the lock picker on a door using the normal trigger mechanism
    /// </summary>
    [HarmonyPatch(typeof(LockPicker), nameof(LockPicker.ItemActivate))]
    [HarmonyPrefix]
    private static bool OnUseItem()
    {
        return Plugin.Config.DisableDoorInteraction.Value;
    }
}

[LCVRPatch]
[HarmonyPatch]
internal static class KeyPatches
{
    /// <summary>
    /// Prevent placing the lock picker on a door using the normal trigger mechanism
    /// </summary>
    [HarmonyPatch(typeof(KeyItem), nameof(KeyItem.ItemActivate))]
    [HarmonyPrefix]
    private static bool OnUseItem()
    {
        return Plugin.Config.DisableDoorInteraction.Value;
    }
}