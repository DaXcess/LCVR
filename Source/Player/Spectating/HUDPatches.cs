using HarmonyLib;
using LCVR.Patches;

namespace LCVR.Player.Spectating;

[LCVRPatch]
[HarmonyPatch]
internal static class HUDPatches
{
    /// <summary>
    /// Make sure the clock is always visible when the player is dead, unless everyone is dead
    /// </summary>
    [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.SetClockVisible))]
    [HarmonyPrefix]
    private static bool DeadPlayerClockAlwaysVisible(HUDManager __instance)
    {
        if (!StartOfRound.Instance.localPlayerController.isPlayerDead || StartOfRound.Instance.allPlayersDead)
            return true;
            
        __instance.Clock.targetAlpha = 1f;
        return false;
    }
}
