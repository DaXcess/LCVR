using HarmonyLib;
using System.Reflection;

namespace LCVR.Patches
{
    /// <summary>
    /// Funny Non-NVIDIA BepInEx Entrypoint quick fix
    /// </summary>
    [LCVRPatch]
    [HarmonyPatch]
    internal static class XRPatches
    {
        private static MethodInfo TargetMethod()
        {
            return AccessTools.Method(AccessTools.TypeByName("UnityEngine.InputSystem.XR.XRSupport"), "Initialize");
        }

        private static bool Prefix()
        {
            return false;
        }
    }
}
