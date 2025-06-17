using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

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
    
    public static void PatchClass(Type type)
    {
        UniversalPatcher.CreateClassProcessor(type, true).Patch();
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
                Logger.LogError($"Failed to apply patches from {type}: {e.Message}, {e.InnerException}");
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

[LCVRPatch(LCVRPatchTarget.Universal)]
[HarmonyPatch]
internal static class HarmonyLibPatches
{
    private static readonly MethodInfo[] ForceUnpatchList =
    [
        AccessTools.PropertySetter(typeof(Camera), nameof(Camera.targetTexture)),
        AccessTools.PropertySetter(typeof(Cursor), nameof(Cursor.visible)),
        AccessTools.PropertySetter(typeof(Cursor), nameof(Cursor.lockState))
    ];
    
    /// <summary>
    /// Ironically, patching harmony like this fixes some issues with unpatching
    /// </summary>
    [HarmonyPatch(typeof(MethodBaseExtensions), nameof(MethodBaseExtensions.HasMethodBody))]
    [HarmonyPrefix]
    private static bool OnUnpatch(MethodBase member, ref bool __result)
    {
        if (!ForceUnpatchList.Contains(member))
            return true;

        __result = true;

        return false;
    }
}
