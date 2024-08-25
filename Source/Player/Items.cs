using LCVR.Items;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LCVR.Player;

internal static class Items
{
    public static readonly Dictionary<string, Type> items = new()
    {
        { "Shovel", typeof(VRShovelItem) },
        { "Yield sign", typeof(VRShovelItem) },
        { "Stop sign", typeof(VRShovelItem) },
        { "Spray paint", typeof(VRSprayPaintItem) },
        { "Pro-flashlight", typeof(VRFlashlight) },
        { "Flashlight", typeof(VRFlashlight) },
        { "Laser pointer", typeof(VRFlashlight) },
        { "Kitchen knife", typeof(VRKnife) },
        { "Maneater", typeof(VRManEaterBaby) }
    };

    public static readonly Dictionary<string, (Vector3, Vector3)> itemOffsets = new()
    {
        { "Chemical jug", (new Vector3(0.2f, -0.18f, 0.24f), new Vector3(270, 180, 0)) },
        { "Toilet paper", (new Vector3(0.15f, -0.16f, 0.17f), new Vector3(270, 180, 0)) },
        { "Boombox", (new Vector3(-0.02f, 0.1f, -0.29f), new Vector3(90, 285, 0)) },
        { "Apparatus", (new Vector3(0.21f, -0.2f, 0.33f), new Vector3(90, 180, 180)) },
        { "Large axle", (new Vector3(0.07f, -0.2f, 0.3f), new Vector3(90, 180, 180)) },
        { "Cash register", (new Vector3(0.05f, -0.29f, 0.11f), new Vector3(270, 132, 288)) },
        { "V-type engine", (new Vector3(-0.10f, -0.06f, 0.26f), new Vector3(66, 190, 0)) },
        { "Extension ladder", (new Vector3(-0.20f, 0.28f, -0.47f), new Vector3(90, 90, 0)) },
        { "Painting", (new Vector3(0.05f, 0.75f, -0.06f), new Vector3(6, 270, 184)) },
        { "Soccer ball", (new Vector3(-0.07f, 0.17f, -0.19f), Vector3.zero) },
        { "Control pad", (new Vector3(0.25f, -0.22f, 0.27f), new Vector3(340, 248, 167)) },
        { "Garbage lid", (new Vector3(0.23f, -0.1f, 0.44f), new Vector3(14, 15, 17)) },
        { "Hive", (new Vector3(0.13f, -0.15f, 0.16f), Vector3.zero) },
        { "Plastic fish", (new Vector3(0, 0.12f, -0.06f), new Vector3(0, 80, 165)) },
        { "Maneater", (new Vector3(0.24f, -0.15f, 0.35f), new Vector3(-6, 167, 14)) }
    };
}
