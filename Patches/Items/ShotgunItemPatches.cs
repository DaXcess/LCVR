using HarmonyLib;
using LCVR.Player;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

using static HarmonyLib.AccessTools;

namespace LCVR.Patches.Items
{
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

            var rayOrigin = Object.FindObjectOfType<VRController>().interactOrigin;
            __instance.ShootGun(rayOrigin.position, rayOrigin.forward);
            typeof(ShotgunItem).GetField("localClientSendingShootGunRPC", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(__instance, true);
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
}
