using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using Mono.Cecil;
using MonoMod.RuntimeDetour;

namespace LCVR.Preload;

public static class Preload
{
    public static IEnumerable<string> TargetDLLs { get; } = [];

    private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("LCVR.Preload");

    public static void Initialize()
    {
        Logger.LogInfo("Patching soft dependencies");

        PatchTypeMethods();

        Logger.LogInfo("We're done here. Goodbye!");
    }

#pragma warning disable CS8618
    // Keep in scope just to be sure the hook stays attached
    private static Hook _getTypesHook;
    private static Hook _isAssignableFromHook;
#pragma warning restore CS8618

    /// <summary>
    /// Hook multiple methods that deal with types so they won't crash if it encounters references to missing assemblies
    /// </summary>
    private static void PatchTypeMethods()
    {
        _getTypesHook = new Hook(AccessTools.Method(typeof(Assembly), nameof(Assembly.GetTypes)),
            AccessTools.Method(typeof(Preload), nameof(GetTypesHook)));

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