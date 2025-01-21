using LCVR.Items;
using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;

namespace LCVR.Player;

internal static class Items
{
    private const int CONFIGURATION_VERSION = 1;
    
    public static readonly Dictionary<string, Type> items = new()
    {
        { "Shovel", typeof(VRShovelItem) },
        { "Yield sign", typeof(VRShovelItem) },
        { "Stop sign", typeof(VRShovelItem) },
        { "Spray paint", typeof(VRSprayPaintItem) },
        { "Kitchen knife", typeof(VRKnife) },
        { "Maneater", typeof(VRManEaterBaby) },
        { "Belt bag", typeof(VRBeltBagItem) }
    };

    public static readonly Dictionary<string, (Vector3, Vector3)> itemOffsets = new()
    {
        { "Chemical jug", (new Vector3(-0.05f, 0.14f, -0.29f), new Vector3(0, 90, 120)) },
        { "Toilet paper", (new Vector3(0, 0.13f, -0.4f), new Vector3(0, 90, 90)) },
        { "Boombox", (new Vector3(-0.02f, 0.1f, -0.29f), new Vector3(90, 285, 0)) },
        { "Apparatus", (new Vector3(-0.04f, 0.12f, -0.21f), new Vector3(0, 270, 0)) },
        { "Large axle", (new Vector3(-0.04f, 0.24f, -0.33f), new Vector3(0, 270, 100)) },
        { "Cash register", (new Vector3(-0.09f, 0.13f, -0.46f), new Vector3(0, 75, 255)) },
        { "V-type engine", (new Vector3(-0.04f, 0.33f, -0.3f), new Vector3(0, 270, 90)) },
        { "Extension ladder", (new Vector3(-0.20f, 0.28f, -0.47f), new Vector3(90, 90, 0)) },
        { "Painting", (new Vector3(0.05f, 0.75f, -0.06f), new Vector3(6, 270, 184)) },
        { "Soccer ball", (new Vector3(-0.07f, 0.17f, -0.19f), Vector3.zero) },
        { "Control pad", (new Vector3(0.06f, 0.09f, -0.23f), new Vector3(90, 90, 0)) },
        { "Garbage lid", (new Vector3(-0.02f, 0.11f, -0.08f), new Vector3(0, 0, 90)) },
        { "Hive", (new Vector3(0.04f, 0.32f, -0.38f), Vector3.zero) },
        { "Plastic fish", (new Vector3(0, 0.12f, -0.06f), new Vector3(0, 80, 165)) },
        { "Belt bag", (new Vector3(0.02f, 0.09f, -0.18f), new Vector3(0, 90, 0)) },
        { "Maneater", (new Vector3(-0.07f, 0.02f, -0.11f), new Vector3(6, 218, 85)) },
        { "Zed Dog", (new Vector3(-0.14f, 0.1f, -0.22f), new Vector3(0, 315, 270)) }
    };

    public static void LoadConfig()
    {
        foreach (var file in Directory.GetFiles(Paths.PluginPath, "*.lcvr-cfg.json", SearchOption.AllDirectories))
        {
            try
            {
                var config = JsonConvert.DeserializeObject<OffsetConfig>(File.ReadAllText(file));

                if (config.version != CONFIGURATION_VERSION)
                {
                    Logger.LogWarning($"{file} is using an unsupported configuration version ({config.version}), file will be ignored");
                    continue;
                }

                if (config.itemOffsets != null)
                    foreach (var (item, value) in config.itemOffsets)
                    {
                        if (itemOffsets.ContainsKey(item))
                            Logger.LogWarning($"Detected duplicate item offset: {item}");
                        
                        itemOffsets[item] = (value.position, value.rotation);
                    }

                if (config.shovels != null)
                    foreach (var shovel in config.shovels)
                    {
                        if (itemOffsets.ContainsKey(shovel))
                            Logger.LogWarning($"Detected duplicate shovel item: {shovel}");

                        items[shovel] = typeof(VRShovelItem);
                    }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Unable to read {file}, ignoring...");
                Logger.LogWarning(ex.Message);
            }
        }
    }
}

[Serializable]
internal struct OffsetConfig
{
    public int version;
    
    [CanBeNull] public Dictionary<string, ItemOffset> itemOffsets;
    [CanBeNull] public string[] shovels;

    [Serializable]
    public struct ItemOffset
    {
        public Vector3 position;
        public Vector3 rotation;
    }
}