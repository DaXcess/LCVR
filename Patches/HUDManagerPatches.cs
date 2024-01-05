using HarmonyLib;

namespace LCVR.Patches
{
    [LCVRPatch]
    [HarmonyPatch]
    internal static class HUDManagerPatches
    {
        /// <summary>
        /// Disables the ping scan if you are in the pause menu
        /// </summary>
        [HarmonyPatch(typeof(HUDManager), "CanPlayerScan")]
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
    }
}
