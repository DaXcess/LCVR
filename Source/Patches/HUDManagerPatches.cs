using HarmonyLib;
using LCVR.Player;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine.InputSystem;

using static HarmonyLib.AccessTools;

namespace LCVR.Patches;

[LCVRPatch]
[HarmonyPatch]
internal static class HUDManagerPatches
{
    /// <summary>
    /// Disables the ping scan if you are in the pause menu
    /// </summary>
    [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.CanPlayerScan))]
    [HarmonyPrefix]
    private static bool CanPlayerScan(ref bool __result)
    {
        if (GameNetworkManager.Instance.localPlayerController.quickMenuManager.isMenuOpen)
        {
            __result = false;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Since the HUD in VR is decoupled from the main HUD element in the game, make sure we manually hide the elements
    /// </summary>
    [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.HideHUD))]
    [HarmonyPostfix]
    private static void HideHUD(bool hide)
    {
        VRSession.Instance.HUD.HideHUD(hide);
    }

    /// <summary>
    /// Fix for the leave early button not working, by making the "PingScan" binding function as the leave early button
    /// </summary>
    [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.Update))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> LeaveEarlyFixTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);
        var startIndex = codes.FindIndex(instruction => instruction.opcode == OpCodes.Callvirt && (MethodInfo)instruction.operand == PropertyGetter(typeof(PlayerActions), "Movement")) - 2;

        var labels = codes[startIndex].labels;
        codes[startIndex++] = new(OpCodes.Callvirt, PropertyGetter(typeof(IngamePlayerSettings), nameof(IngamePlayerSettings.Instance)))
        {
            labels = labels
        };

        codes[startIndex++] = new(OpCodes.Ldfld, Field(typeof(IngamePlayerSettings), nameof(IngamePlayerSettings.playerInput)));
        codes[startIndex++] = new(OpCodes.Callvirt, PropertyGetter(typeof(PlayerInput), nameof(PlayerInput.actions)));
        codes[startIndex++] = new(OpCodes.Ldstr, "PingScan");
        codes[startIndex++] = new(OpCodes.Ldc_I4_0);
        codes[startIndex++] = new(OpCodes.Callvirt, Method(typeof(InputActionAsset), nameof(InputActionAsset.FindAction), [typeof(string), typeof(bool)]));
        codes[startIndex++] = new(OpCodes.Callvirt, Method(typeof(InputAction), nameof(InputAction.IsPressed)));

        return codes.AsEnumerable();
    }
}