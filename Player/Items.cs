using LCVR.Items;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LCVR.Player
{
    internal class Items
    {
        public static readonly string[] unsupportedItems = ["Jetpack"];
        public static readonly Dictionary<string, Type> items = new()
        {
            { "Shovel", typeof(VRShovelItem) },
            { "Yield sign", typeof(VRShovelItem) },
            { "Stop sign", typeof(VRShovelItem) },
            { "Spray paint", typeof(VRSprayPaintItem) },
            { "Pro-flashlight", typeof(VRFlashlight) },
            { "Flashlight", typeof(VRFlashlight) },
            { "Laser pointer", typeof(VRFlashlight) },
        };

        public static void UpdateVRControlsItemsOffsets()
        {
            foreach (var item in StartOfRound.Instance.allItemsList.itemsList)
            {
#if DEBUG
                item.canBeGrabbedBeforeGameStart = true;
#endif

                if (item.itemName == "Chemical Jug")
                {
                    item.positionOffset = new Vector3(-0.1f, 0.18f, -0.24f);
                    item.rotationOffset = new Vector3(180, 287.52f, 0);
                }
                else if (item.itemName == "Boombox")
                {
                    item.positionOffset = new Vector3(0.1f, 0, -0.03f);
                }
            }
        }
    }
}
