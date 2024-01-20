using System.Collections.Generic;
using System.Linq;

namespace LCVR
{
    public class Compat
    {
        private static readonly CompatibleMod[] ModCompatibilityList =
        [
            new("MoreCompany", "me.swipez.melonloader.morecompany", "1.7.4", "1.7.5", "1.7.6"),
            new("Mimics", "x753.Mimics", "2.3.2"),
            new("TooManyEmotes", "FlipMods.TooManyEmotes", "1.7.19")
        ];

        private static readonly List<string> DetectedMods = [];

        public Compat(BepInEx.PluginInfo[] plugins)
        {
            foreach (var plugin in plugins)
            {
                var mod = ModCompatibilityList.FirstOrDefault((mod) => mod.Guid == plugin.Metadata.GUID);

                if (mod == null)
                    continue;

                if ((mod.Versions == null || !mod.Versions.Contains(plugin.Metadata.Version.ToString())) && !Plugin.Config.OverrideCompatibilityVersionCheck.Value)
                    continue;

                Logger.LogInfo($"Found compatible mod {mod.Name}");

                DetectedMods.Add(mod.Name);
            }
        }

        public bool IsLoaded(string modName)
        {
            return DetectedMods.Contains(modName);
        }

        private class CompatibleMod(string name, string guid, params string[] versions)
        {
            public string Name { get; } = name;
            public string Guid { get; } = guid;
            public string[] Versions { get; } = versions;
        }
    }
}
