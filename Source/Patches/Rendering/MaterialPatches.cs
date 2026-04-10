using HarmonyLib;
using LCVR.Assets;
using UnityEngine;

namespace LCVR.Patches.Rendering;

[LCVRPatch]
[HarmonyPatch]
internal static class MaterialPatches
{
    /// <summary>
    /// Replace some ship particles once level loads
    /// </summary>
    [HarmonyPatch(typeof(Terminal), nameof(Terminal.Awake))]
    [HarmonyPostfix]
    private static void OnShipLevelLoad()
    {
        var giantMagnet = GameObject.Find("GiantCylinderMagnet").transform;

        var magnetParticle1 = giantMagnet.Find("MagnetParticle").GetComponent<ParticleSystemRenderer>();
        var magnetParticle2 =
            magnetParticle1.transform.Find("MagnetParticle (1)").GetComponent<ParticleSystemRenderer>();
        var magnetParticle3 =
            magnetParticle1.transform.Find("MagnetParticle (2)").GetComponent<ParticleSystemRenderer>();

        var chargerParticle = GameObject.Find("ShipModels2b").transform.Find("ChargeStation/ZapParticle")
            .GetComponent<ParticleSystemRenderer>();

        magnetParticle1.material = magnetParticle2.material =
            AssetManager.assetsBundle.LoadAsset<Material>("LightningSpriteSheetMaterial3");
        magnetParticle3.material = AssetManager.assetsBundle.LoadAsset<Material>("LightningSpriteSheetMaterial2");
        chargerParticle.material = AssetManager.assetsBundle.LoadAsset<Material>("LightningSpriteSheetMaterial");
    }
}
