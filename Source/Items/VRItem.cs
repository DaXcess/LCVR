using GameNetcodeStuff;
using LCVR.Networking;
using System.Collections.Generic;
using UnityEngine;

namespace LCVR.Items;

/// <summary>
/// A special script that gets added to certain items in the game.
/// This allows the mod to manipulate the items with custom update loops, 
/// and prevent some built in game code from running on them.
/// </summary>
public abstract class VRItem<T> : MonoBehaviour where T : GrabbableObject
{
    public static readonly Dictionary<GrabbableObject, VRItem<T>> itemCache = [];

    protected T item;
    protected PlayerControllerB player;
    protected VRNetPlayer networkPlayer;

    /// <summary>
    /// Keep receiving updates even when the item is pocketed
    /// </summary>
    protected bool UpdateWhenPocketed { get; set; }

    protected bool IsLocal { get; private set; }

    protected virtual void Awake()
    {
        try
        {
            item = GetComponent<T>();
            player = item.playerHeldBy;
            networkPlayer = player.GetComponent<VRNetPlayer>();
            IsLocal = networkPlayer == null;

            Logger.LogDebug(
                $"VRItem[{GetType().Name}] (IsLocal: {IsLocal}) instantiated for item '{item.itemProperties.itemName}'");

            itemCache.Add(item, this);
        }
        catch
        {
            Logger.LogError(
                $"Failed to create VRItem[{GetType().Name}], player probably joined through LateCompany or LobbyControl");

            Destroy(this);
        }
    }

    protected virtual void LateUpdate()
    {
        if (item.playerHeldBy != player || (!UpdateWhenPocketed && item.isPocketed) || !item.isHeld)
        {
            Logger.LogDebug($"VRItem[{GetType().Name}] (IsLocal: {IsLocal}) is being destroyed");
            itemCache.Remove(item);

            Destroy(this);
            return;
        }

        OnUpdate();
    }

    protected abstract void OnUpdate();

    protected bool CanUseItem()
    {
        return item.playerHeldBy.CanUseItem();
    }
}
