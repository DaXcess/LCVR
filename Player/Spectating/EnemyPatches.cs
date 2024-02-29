using HarmonyLib;
using LCVR.Patches;

namespace LCVR.Player.Spectating;

/// <summary>
/// Enemy specific patches for the free roam spectator functionality
/// </summary>
[LCVRPatch]
[HarmonyPatch]
internal static class SpectatorEnemyPatches
{
    /// <summary>
    /// Prevent the nutcracker from seeing the local player if they're dead
    /// </summary>
    [HarmonyPatch(typeof(NutcrackerEnemyAI), nameof(NutcrackerEnemyAI.CheckLineOfSightForLocalPlayer))]
    [HarmonyPrefix]
    private static bool NutcrackerCheckForLocalPlayer(ref bool __result)
    {
        if (StartOfRound.Instance.localPlayerController.isPlayerDead)
        {
            __result = false;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Prevent detection by centipedes that are hidden on the ceiling
    /// </summary>
    [HarmonyPatch(typeof(CentipedeAI), nameof(CentipedeAI.TriggerCentipedeFallServerRpc))]
    [HarmonyPrefix]
    private static bool TriggerCentipedeFall(CentipedeAI __instance)
    {
        var networkManager = __instance.NetworkManager;

        if ((networkManager.IsClient || networkManager.IsHost) && StartOfRound.Instance.localPlayerController.isPlayerDead)
        {
            AccessTools.Field(typeof(CentipedeAI), "triggeredFall").SetValue(__instance, false);
            return false;
        }

        return true;
    }
}