using GameNetcodeStuff;
using LCVR.Player;
using UnityEngine;

namespace LCVR.Items
{
    internal abstract class VRItem<T> : MonoBehaviour where T: GrabbableObject
    {
        protected T item;
        protected PlayerControllerB player;

        protected virtual void Awake()
        {
            item = GetComponent<T>();
            player = VRPlayer.Instance.GetComponent<PlayerControllerB>();

            Logger.LogDebug($"VRItem[{GetType()}] instantiated for item {item.itemProperties.itemName}");
        }

        protected virtual void LateUpdate()
        {
            if (item.playerHeldBy != player)
            {
                enabled = false;
                return;
            }

            OnUpdate();
        }

        protected abstract void OnUpdate();
    }
}
