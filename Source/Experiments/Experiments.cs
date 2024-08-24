using HarmonyLib;
using LCVR.Patches;
using System.Collections.Generic;
using System.Linq;
using LCVR.Assets;
using Unity.Netcode;
using UnityEngine;

namespace LCVR.Experiments;

internal static class Experiments
{
    public static void RunExperiments()
    {
        // ShowMeTheMoney(10000);
        // SpawnShotgun();
        // SpawnBuyableItem<JetpackItem>("Jetpack");
        // SpawnBuyableItem<SprayPaintItem>("Spray paint");
        // SpawnBuyableItem<FlashlightItem>("Flashlight");
        // SpawnBuyableItem<FlashlightItem>("Pro-flashlight");
        // SpawnBuyableItem<StunGrenadeItem>("Stun grenade");
        // SpawnBuyableItem<PatcherTool>("Zap gun");
        //SpawnBuyableItem<WalkieTalkie>("Walkie-talkie");

        // SpawnNonBuyableItem(["Laser pointer"]);
    }

    private static void SpawnShotgun()
    {
        var position = new Vector3(-1.4374f, 3.643f, -14.1965f);

        var level = StartOfRound.Instance.levels.First((level) => level.PlanetName == "8 Titan");
        var nutcracker = level.Enemies.Find((enemy) => enemy.enemyType.enemyName == "Nutcracker");
        var ncai = nutcracker.enemyType.enemyPrefab.gameObject.GetComponent<NutcrackerEnemyAI>();

        var shotgun = SpawnObject<ShotgunItem>(ncai.gunPrefab);
        shotgun.shellsLoaded = 500;
    }

    private static void ShowMeTheMoney(int amount)
    {
        var terminal = Object.FindObjectOfType<Terminal>();

        if (!terminal.IsOwner)
        {
            Logger.LogWarning("Cannot spawn in more credits: You must be server host");
            return;
        }
        
        terminal.groupCredits = amount;
        terminal.SyncGroupCreditsServerRpc(terminal.groupCredits, terminal.numberOfItemsInDropship);
    }

    private static void SpawnNonBuyableItem(List<string> @itemNames)
    {
        foreach (var item in StartOfRound.Instance.allItemsList.itemsList)
        {
            if (itemNames.Contains(item.itemName))
            {
                SpawnObject<GrabbableObject>(item.spawnPrefab);
            }
        }
    }
    private static void SpawnBuyableItem<T>(string @itemName)
        where T : GrabbableObject
    {
        var terminal = Object.FindObjectOfType<Terminal>();
        var i = terminal.buyableItemsList.First((item) => item.itemName == @itemName);

        if (i != null)
        {
            SpawnObject<T>(i.spawnPrefab);
        }
    }

    private static T SpawnObject<T>(GameObject @object)
    where T : GrabbableObject
    {
        var gameObject = Object.Instantiate(@object, new Vector3(-1.4374f, 3.643f, -14.1965f), Quaternion.identity, RoundManager.Instance.spawnedScrapContainer);
        var component = gameObject.GetComponent<T>();
        component.transform.rotation = Quaternion.Euler(component.itemProperties.restingRotation);
        component.fallTime = 0f;
        component.scrapValue = 10;

        var netComponent = gameObject.GetComponent<NetworkObject>();
        netComponent.Spawn(false);
        return component;
    }
}

internal static class DebugLinePool
{
    private static Dictionary<string, LineRenderer> lines = [];

    public static LineRenderer GetLine(string key)
    {
        if (lines.TryGetValue(key, out var line))
        {
            if (line != null)
                return line;
        }

        line = CreateRenderer();
        lines[key] = line;

        return line;
    }
    
    private static LineRenderer CreateRenderer()
    {
        var gameObject = new GameObject("DebugLinePool Line Renderer");
        
        var lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.widthCurve.keys = [new Keyframe(0, 1)];
        lineRenderer.widthMultiplier = 0.005f;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPositions(new[] { Vector3.zero, Vector3.zero });
        lineRenderer.numCornerVertices = 4;
        lineRenderer.numCapVertices = 4;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.shadowBias = 0.5f;
        lineRenderer.useWorldSpace = true;
        lineRenderer.maskInteraction = SpriteMaskInteraction.None;
        lineRenderer.SetMaterials([AssetManager.DefaultRayMat]);
        lineRenderer.enabled = true;

        return lineRenderer;
    }
}

#if DEBUG
[LCVRPatch(LCVRPatchTarget.Universal)]
[HarmonyPatch]
internal static class ExperimentalPatches
{
    /// <summary>
    /// Enables the game's built in debug mode
    /// </summary>
    [HarmonyPatch(typeof(Application), nameof(Application.isEditor), MethodType.Getter)]
    [HarmonyPrefix]
    private static bool DeveloperMode(ref bool __result)
    {
        __result = true;

        return false;
    }
}
#endif

// TODO: Remove yes
// [LCVRPatch]
[HarmonyPatch]
internal static class ItemOffsetEditorPatches
{
    private static Transform _offset;
    private static Transform Offset =>
        _offset == null ? _offset = new GameObject("VR Item Offset Editor").transform : _offset;

    [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.LateUpdate))]
    [HarmonyPrefix]
    private static bool OverrideItemOffset(GrabbableObject __instance)
    {
        return true;
        
        if (!Offset.gameObject.activeSelf)
            return true;
        
        if (__instance.parentObject != null)
        {
            var tf = __instance.transform;
            
            tf.rotation = __instance.parentObject.rotation;
            tf.Rotate(Offset.eulerAngles);
            tf.position = __instance.parentObject.position + __instance.parentObject.rotation * Offset.position;
        }
        
        if (__instance.radarIcon != null)
        {
            __instance.radarIcon.position = __instance.transform.position;
        }

        return false;
    }
    
    [HarmonyPatch(typeof(CaveDwellerPhysicsProp), nameof(CaveDwellerPhysicsProp.LateUpdate))]
    [HarmonyPrefix]
    private static bool OverrideCaveDwellerItemOffset(CaveDwellerPhysicsProp __instance)
    {
        return true;
        
        if (!Offset.gameObject.activeSelf)
            return true;
        
        if (__instance.caveDwellerScript.inSpecialAnimation && __instance.parentObject != null)
        {
            var tf = __instance.transform;
            
            tf.rotation = __instance.parentObject.rotation;
            tf.Rotate(Offset.eulerAngles);
            tf.position = __instance.parentObject.position + __instance.parentObject.rotation * Offset.position;
        }
        
        if (__instance.radarIcon != null)
        {
            __instance.radarIcon.position = __instance.transform.position;
        }

        return false;
    }
}