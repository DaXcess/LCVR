using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Configuration.Assemblies;
using System.Reflection;
using System.Text;

namespace LCVR.Patches
{
    [LCVRPatch(LCVRPatchTarget.Universal)]
    [HarmonyPatch]
    internal static class AssemblyPatches
    {
        [HarmonyPatch(typeof(Assembly), "Load", [typeof(string)])]
        [HarmonyPrefix]
        private static void OnAssemblyLoad(string assemblyString)
        {
            Logger.LogDebug($"[Assembly]::Load assemblyString = {assemblyString}");
        }

        [HarmonyPatch(typeof(Assembly), "Load", [typeof(AssemblyName)])]
        [HarmonyPrefix]
        private static void OnAssemblyLoad(AssemblyName assemblyRef)
        {
            Logger.LogDebug($"[Assembly]::Load assemblyRef = {assemblyRef.FullName}");
        }

        [HarmonyPatch(typeof(AppDomain), "Load", [typeof(string)])]
        [HarmonyPrefix]
        private static void OnAppDomainLoad(string assemblyString)
        {
            Logger.LogDebug($"[AppDomain]::Load assemblyString = {assemblyString}");
        }

        [HarmonyPatch(typeof(AppDomain), "Load", [typeof(AssemblyName)])]
        [HarmonyPrefix]
        private static void OnAppDomainLoad(AssemblyName assemblyRef)
        {
            Logger.LogDebug($"[AppDomain]::Load assemblyRef = {assemblyRef.FullName}");
        }

        [HarmonyPatch(typeof(Assembly), "LoadFrom", [typeof(string)])]
        [HarmonyPrefix]
        private static void OnAssemblyLoadFrom(string assemblyFile)
        {
            Logger.LogDebug($"[Assembly]::LoadFrom assemblyFile = {assemblyFile}");
        }

        [HarmonyPatch(typeof(Assembly), "LoadFrom", [typeof(string), typeof(byte[]), typeof(AssemblyHashAlgorithm)])]
        [HarmonyPrefix]
        private static void OnAssemblyLoadNamePrefix(string assemblyFile)
        {
            Logger.LogDebug($"[Assembly]::LoadFrom(3) assemblyFile = {assemblyFile}");
        }

        [HarmonyPatch(typeof(Assembly), "LoadFile", [typeof(string)])]
        [HarmonyPrefix]
        private static void OnAssemblyLoadFile(string path)
        {
            Logger.LogDebug($"[Assembly]::LoadFile path = {path}");
        }
    }
}
