using HarmonyLib;
using LCVR.Player;

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
}
