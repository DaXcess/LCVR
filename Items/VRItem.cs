using GameNetcodeStuff;
using HarmonyLib;
using LCVR.Networking;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace LCVR.Items
{
    /// <summary>
    /// A special script that gets added to certain items in the game.
    /// This allows the mod to manipulate the items with custom update loops, 
    /// and prevent some built in game code from running on them.
    /// </summary>
    internal abstract class VRItem<T> : MonoBehaviour where T : GrabbableObject
    {
        public static readonly Dictionary<GrabbableObject, VRItem<T>> itemCache = [];

        private static readonly MethodInfo canUseItem = AccessTools.Method(typeof(PlayerControllerB), "CanUseItem");

        protected T item;
        protected PlayerControllerB player;
        protected VRNetPlayer networkPlayer;

        /// <summary>
        /// Keep receiving updates even when the item is pocketed
        /// </summary>
        protected bool UpdateWhenPocketed { get; set; } = false;

        /// <summary>
        /// Prevents the game from running LateUpdate calls on this item, which mess with the position and rotation of the object
        /// </summary>
        public bool CancelGameUpdate { get; protected set; } = false;

        protected bool IsLocal
        {
            get => _isLocal;
        }

        private bool _isLocal = false;

        protected virtual void Awake()
        {
            item = GetComponent<T>();
            player = item.playerHeldBy;
            networkPlayer = player.GetComponent<VRNetPlayer>();
            _isLocal = networkPlayer == null;

            itemCache.Add(item, this);

            Logger.LogDebug($"VRItem[{GetType().Name}] (IsLocal: {_isLocal}) instantiated for item '{item.itemProperties.itemName}'");
        }

        protected virtual void LateUpdate()
        {
            if (item.playerHeldBy != player || (!UpdateWhenPocketed && item.isPocketed) || !item.isHeld)
            {
                Logger.LogDebug($"VRItem[{GetType().Name}] (IsLocal: {_isLocal}) is being destroyed");
                itemCache.Remove(item);

                Destroy(this);
                return;
            }

            OnUpdate();
        }

        protected abstract void OnUpdate();

        protected bool CanUseItem()
        {
            return (bool)canUseItem.Invoke(item.playerHeldBy, []);
        }
    }
}
