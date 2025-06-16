using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using LCVR.Managers;

namespace LCVR.Patches;

[LCVRPatch(LCVRPatchTarget.Universal)]
[HarmonyPatch]
internal static class UniversalCarPatches
{
    /// <summary>
    /// Keep track of vehicle instances that get removed from the game
    /// </summary>
    [HarmonyPatch(typeof(VehicleController), nameof(VehicleController.OnDisable))]
    [HarmonyPostfix]
    private static void OnCarDestroyed(VehicleController __instance)
    {
        VRSession.Instance.CarManager.OnCarDestroyed(__instance);
    }
}

[LCVRPatch]
[HarmonyPatch]
internal static class VRCarPatches
{
    /// <summary>
    /// Replace vanilla vehicle input handler with our own
    /// </summary>
    [HarmonyPatch(typeof(VehicleController), nameof(VehicleController.GetVehicleInput))]
    [HarmonyPrefix]
    private static bool HookGetVehicleInput(VehicleController __instance)
    {
        if (Plugin.Config.DisableCarSteeringWheelInteraction.Value)
            return true;
        
        var wheel = VRSession.Instance.CarManager.FindWheelForVehicle(__instance);
        if (wheel)
            wheel.GetSteeringInput();
        
        return false;
    }

    /// <summary>
    /// Remove player body animations from car update loop as we are controlling these values ourselves
    /// </summary>
    [HarmonyPatch(typeof(VehicleController), nameof(VehicleController.Update))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> RemovePlayerAnimations(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchForward(false, [new CodeMatch(OpCodes.Ldstr, "SA_CarAnim")])
            .Advance(-23)
            // Keep single nop since this instruction can be jumped to
            .SetOpcodeAndAdvance(OpCodes.Nop)
            .RemoveInstructions(39)
            .InstructionEnumeration();
    }
}
