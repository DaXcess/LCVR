using HarmonyLib;

namespace LCVR.Patches
{
    [LCVRPatch]
    [HarmonyPatch(typeof(HUDManager), "CanPlayerScan")]
    internal static class HUDManagerPatches
    {
        /// <summary>
        /// Disables the ping scan if you are in the pause menu
        /// </summary>
        private static bool Prefix(ref bool __result)
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
