using HarmonyLib;
using System;
using System.Reflection;

namespace LCVR.Patches
{
    internal static class HarmonyPatcher
    {
        private static readonly Harmony vrPatcher = new("LCVR-VROnly");
        private static readonly Harmony universalPatcher = new("LCVR-Universal");

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
                var attribute = (LCVRPatchAttribute)Attribute.GetCustomAttribute(type, typeof(LCVRPatchAttribute));

                if (attribute == null)
                    return;

                if (attribute.Dependency != null && !Plugin.Compatibility.IsLoaded(attribute.Dependency))
                    return;

                if (attribute.Target == target)
                    patcher.CreateClassProcessor(type).Patch();
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
