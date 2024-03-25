using System.Collections.Generic;
using System.Linq;

namespace LCVR;

public class Compat
{
    private static readonly CompatibleMod[] ModCompatibilityList =
    [
        new("MoreCompany", "me.swipez.melonloader.morecompany"),
        new("Mimics", "x753.Mimics"),
        new("Diversity", "Chaos.Diversity"),
    ];

    private static readonly List<string> DetectedMods = [];

    public Compat(BepInEx.PluginInfo[] plugins)
    {
        foreach (var plugin in plugins)
        {
            var mod = ModCompatibilityList.FirstOrDefault((mod) => mod.Guid == plugin.Metadata.GUID);

            if (mod == null)
                continue;

            Logger.LogInfo($"Found compatible mod {mod.Name}");

            DetectedMods.Add(mod.Name);
        }
    }

    public bool IsLoaded(string modName)
    {
        return DetectedMods.Contains(modName);
    }

    private class CompatibleMod(string name, string guid)
    {
        public string Name { get; } = name;
        public string Guid { get; } = guid;
    }
}
