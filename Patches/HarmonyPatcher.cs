using HarmonyLib;
using System;
using System.Reflection;

namespace LCVR.Patches
{
    internal static class HarmonyPatcher
    {

        private static readonly Harmony vrPatcher = new("io.daxcess.lcvr");
        private static readonly Harmony universalPatcher = new("io.daxcess.lcvr-universal");

        public static void PatchUniversal()
        {
            Patch(universalPatcher, LCVRPatchTarget.Universal);
        }

        public static void PatchVR()
        {
            Patch(vrPatcher, LCVRPatchTarget.VROnly);
        }

        private static void Patch(Harmony patcher, LCVRPatchTarget target)
        {
            AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly()).Do((type) =>
            {
                try
                {
                    var attribute = (LCVRPatchAttribute)Attribute.GetCustomAttribute(type, typeof(LCVRPatchAttribute));

                    if (attribute == null)
                        return;

                    if (attribute.Dependency != null && !Plugin.Compatibility.IsLoaded(attribute.Dependency))
                        return;

                    if (attribute.Target == target)
                        patcher.CreateClassProcessor(type).Patch();
                }
                catch (Exception e)
                {
                    Logger.LogError($"Failed to apply patches from {type}: {e.Message}");
                }
            });
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    internal class LCVRPatchAttribute(LCVRPatchTarget target = LCVRPatchTarget.VROnly, string dependency = null) : Attribute
    {
        public LCVRPatchTarget Target { get; } = target;
        public string Dependency { get; } = dependency;
    }

    internal enum LCVRPatchTarget
    {
        Universal,
        VROnly
    }
}
