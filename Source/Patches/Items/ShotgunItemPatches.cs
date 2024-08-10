using HarmonyLib;
using LCVR.Player;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace LCVR.Patches.Items;

[LCVRPatch]
[HarmonyPatch]
internal static class ShotgunItemPatches
{
    /// <summary>
    /// Makes the shotgun shoot from your hand instead of your head
    /// </summary>
    [HarmonyPatch(typeof(ShotgunItem), nameof(ShotgunItem.ShootGunAndSync))]
    [HarmonyPrefix]
    private static bool OnShootGun(ShotgunItem __instance, bool heldByPlayer)
    {
        if (!heldByPlayer)
            return true;

        var rayOrigin = VRSession.Instance.LocalPlayer.PrimaryController.InteractOrigin;
        __instance.ShootGun(rayOrigin.position, rayOrigin.forward);
        __instance.localClientSendingShootGunRPC = true;
        __instance.ShootGunServerRpc(rayOrigin.position, rayOrigin.forward);

        return false;
    }

    /// <summary>
    /// Allows the player to shoot themselves in VR
    /// </summary>
    [HarmonyPatch(typeof(ShotgunItem), nameof(ShotgunItem.ShootGun))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> KurtCobainTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchForward(false,
                new CodeMatch(OpCodes.Callvirt,
                    AccessTools.Method(typeof(Animator), nameof(Animator.SetTrigger), [typeof(string)])))
            .Advance(1)
            .SetOpcodeAndAdvance(OpCodes.Ldc_I4_0)
            .InstructionEnumeration();
    }
}