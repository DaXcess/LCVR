using HarmonyLib;
using LCVR.Player;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

using static HarmonyLib.AccessTools;

namespace LCVR.Patches.Items;

[LCVRPatch]
[HarmonyPatch]
internal static class ShotgunItemPatches
{
    /// <summary>
    /// Makes the shotgun shoot from your hand instead of your head
    /// </summary>
    [HarmonyPatch(typeof(ShotgunItem), "ShootGunAndSync")]
    [HarmonyPrefix]
    private static bool OnShootGun(ShotgunItem __instance, bool heldByPlayer)
    {
        if (!heldByPlayer)
            return true;

        var rayOrigin = VRSession.Instance.LocalPlayer.PrimaryController.InteractOrigin;
        __instance.ShootGun(rayOrigin.position, rayOrigin.forward);
        Field(typeof(ShotgunItem), "localClientSendingShootGunRPC").SetValue(__instance, true);
        __instance.ShootGunServerRpc(rayOrigin.position, rayOrigin.forward);

        return false;
    }
}

/// <summary>
/// Allows the player to shoot themselves in VR
/// </summary>
[LCVRPatch]
[HarmonyPatch(typeof(ShotgunItem), "ShootGun")]
internal static class KurtCobainPatches
{
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);

        int index = codes.FindIndex(x => x.operand == (object)Method(typeof(Animator), nameof(Animator.SetTrigger), [typeof(string)])) + 1;
        
        codes[index].opcode = OpCodes.Ldc_I4_0;

        return codes.AsEnumerable();
    }
}
