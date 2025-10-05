using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine.Rendering;

namespace LCVR.Patches;

[LCVRPatch(LCVRPatchTarget.Universal)]
[HarmonyPatch]
internal static class AssemblyTypePatches
{
    [HarmonyPatch(typeof(CoreUtils), nameof(CoreUtils.GetAllAssemblyTypes))]
    [HarmonyPrefix]
    private static bool ReplaceGetAllAssemblyTypes(ref IEnumerable<Type> __result)
    {
        __result = GetAllAssemblyTypes();
        return false;
    }

    private static IEnumerable<Type> GetAllAssemblyTypes()
    {
        CoreUtils.m_AssemblyTypes ??= AppDomain.CurrentDomain.GetAssemblies().SelectMany(delegate(Assembly a)
        {
            try
            {
                return a.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null);
            }
        });

        return CoreUtils.m_AssemblyTypes;
    }
}