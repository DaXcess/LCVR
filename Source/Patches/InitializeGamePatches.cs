using HarmonyLib;

namespace LCVR.Patches;

[LCVRPatch]
[HarmonyPatch]
internal static class InitializeGamePatches
{
    /// <summary>
    /// Show the cold open cinematic if this is the first time playing VR
    /// </summary>
    [HarmonyPatch(typeof(InitializeGame), nameof(InitializeGame.Awake))]
    [HarmonyPostfix]
    private static void AlwaysCutscene(InitializeGame __instance)
    {
        if (!__instance.playColdOpenCinematic)
            __instance.playColdOpenCinematic = !Plugin.Config.IntroScreenSeen.Value;
    }
}