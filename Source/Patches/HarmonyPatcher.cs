using HarmonyLib;
using System;
using System.Reflection;
using LCVR.Experiments;

namespace LCVR.Patches;

internal static class HarmonyPatcher
{
    private static readonly Harmony VRPatcher = new("io.daxcess.lcvr");
    private static readonly Harmony UniversalPatcher = new("io.daxcess.lcvr-universal");

    public static void PatchUniversal()
    {
        Patch(UniversalPatcher, LCVRPatchTarget.Universal);
    }

    public static void PatchVR()
    {
        Patch(VRPatcher, LCVRPatchTarget.VROnly);

        if (!Plugin.Flags.HasFlag(Flags.ItemOffsetEditor))
            return;

        Logger.LogWarning("Item offset editor is enabled!");
        VRPatcher.CreateClassProcessor(typeof(ItemOffsetEditorPatches)).Patch();
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

                if (attribute.Dependency != null && !Compat.IsLoaded(attribute.Dependency))
                    return;

                if (attribute.Target != target)
                    return;

                Logger.LogDebug($"Applying patches from: {type.FullName}");
                    
                patcher.CreateClassProcessor(type).Patch();
            }
            catch (Exception e)
            {
                Logger.LogError($"Failed to apply patches from {type}: {e.Message}");
            }
        });
    }
}

[AttributeUsage(AttributeTargets.Class)]
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
