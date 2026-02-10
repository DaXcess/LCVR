using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Mono.Cecil;
using MonoMod.RuntimeDetour;

namespace LCVR.Preload;

public static class Preload
{
    public static IEnumerable<string> TargetDLLs { get; } = [];

    private const string VR_MANIFEST = """
                                       {
                                         "name": "OpenXR XR Plugin",
                                         "version": "1.8.2",
                                         "libraryName": "UnityOpenXR",
                                         "displays": [
                                           {
                                             "id": "OpenXR Display"
                                           }
                                         ],
                                         "inputs": [
                                           {
                                             "id": "OpenXR Input"
                                           }
                                         ]
                                       }
                                       """;

    private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("LCVR.Preload");

    public static void Initialize()
    {
        Logger.LogInfo("Setting up VR runtime assets");

        SetupRuntimeAssets();
        PatchTypeMethods();

        Logger.LogInfo("We're done here. Goodbye!");
    }

    /// <summary>
    /// Place required runtime libraries and configuration in the game files to allow VR to be started
    /// </summary>
    private static void SetupRuntimeAssets()
    {
        var root = Path.Combine(Paths.GameRootPath, "Lethal Company_Data");
        var subsystems = Path.Combine(root, "UnitySubsystems");
        if (!Directory.Exists(subsystems))
            Directory.CreateDirectory(subsystems);

        var openXr = Path.Combine(subsystems, "UnityOpenXR");
        if (!Directory.Exists(openXr))
            Directory.CreateDirectory(openXr);

        var manifest = Path.Combine(openXr, "UnitySubsystemsManifest.json");
        if (!File.Exists(manifest))
            File.WriteAllText(manifest, VR_MANIFEST);

        var plugins = Path.Combine(root, "Plugins");
        var oxrPluginTarget = Path.Combine(plugins, "UnityOpenXR.dll");
        var oxrLoaderTarget = Path.Combine(plugins, "openxr_loader.dll");

        var current = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var oxrPlugin = Path.Combine(current, "RuntimeDeps/UnityOpenXR.dll");
        var oxrLoader = Path.Combine(current, "RuntimeDeps/openxr_loader.dll");

        if (!CopyResourceFile(oxrPlugin, oxrPluginTarget))
            Logger.LogWarning("Could not find plugin UnityOpenXR.dll, VR might not work!");

        if (!CopyResourceFile(oxrLoader, oxrLoaderTarget))
            Logger.LogWarning("Could not find plugin openxr_loader.dll, VR might not work!");
    }

    /// <summary>
    /// Helper function for SetupRuntimeAssets() to copy resource files and return false if the source does not exist
    /// </summary>
    private static bool CopyResourceFile(string sourceFile, string destinationFile)
    {
        if (!File.Exists(sourceFile))
            return false;

        if (File.Exists(destinationFile))
        {
            var sourceHash = Utils.ComputeHash(File.ReadAllBytes(sourceFile));
            var destHash = Utils.ComputeHash(File.ReadAllBytes(destinationFile));

            if (sourceHash.SequenceEqual(destHash))
                return true;
        }

        File.Copy(sourceFile, destinationFile, true);

        return true;
    }

#pragma warning disable CS8618
    // Keep in scope just to be sure the hook stays attached
    private static Hook _getTypesHook;
    private static Hook _isAssignableFromHook;
#pragma warning restore CS8618

    /// <summary>
    /// Hook Assembly.GetTypes() so it won't crash if it encounters references to missing assemblies
    /// </summary>
    private static void PatchTypeMethods()
    {
        // TODO: Remove if it's determined that this is not needed
        // _getTypesHook = new Hook(typeof(Assembly).GetMethod("GetTypes", BindingFlags.Instance | BindingFlags.Public),
        //     typeof(Preload).GetMethod(nameof(GetTypesHook)));

        _isAssignableFromHook =
            new Hook(
                AccessTools.Method(AccessTools.TypeByName("System.RuntimeType"), "IsAssignableFrom", [typeof(Type)]),
                AccessTools.Method(typeof(Preload), nameof(IsAssignableFromHook)));
    }

    private static Type[] GetTypesHook(Func<Assembly, Type[]> orig, Assembly self)
    {
        try
        {
            return orig(self).Where(t => t != null).ToArray();
        }
        catch (ReflectionTypeLoadException e)
        {
            return e.Types.Where(t => t != null).ToArray();
        }
    }

    private static bool IsAssignableFromHook(Func<Type, Type, bool> orig, Type self, Type c)
    {
        try
        {
            return orig(self, c);
        }
        catch (TypeLoadException)
        {
            return false;
        }
    }

    public static void Patch(AssemblyDefinition assembly)
    {
        // No-op
    }
}