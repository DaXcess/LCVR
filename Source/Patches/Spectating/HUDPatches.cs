using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using LCVR.Managers;
using UnityEngine.InputSystem;

using static HarmonyLib.AccessTools;

namespace LCVR.Patches.Spectating;

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

    /// <summary>
    /// Disable voting by using the keybind in favor of using the spectator menu
    /// </summary>
    [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.Update))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> DisablePingVotePatch(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchForward(false,
                new CodeMatch(OpCodes.Callvirt, Method(typeof(InputAction), nameof(InputAction.IsPressed))))
            .Advance(-6)
            .SetOpcodeAndAdvance(OpCodes.Ldc_I4_0)
            .RemoveInstructions(6)
            .InstructionEnumeration();
    }

    /// <summary>
    /// Update the custom spectate UI
    /// </summary>
    [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.UpdateBoxesSpectateUI))]
    [HarmonyPostfix]
    private static void OnUpdateSpectateUI()
    {
        VRSession.Instance.HUD.SpectatingMenu.UpdateBoxes();
    }
}
