using System.Collections.Generic;
using System.Linq;

namespace LCVR
{
    public class Compat
    {
        private static readonly string[][] ModCompatibilityList =
        [
            ["MoreCompany", "me.swipez.melonloader.morecompany", "1.7.2"]
        ];

        private static readonly List<string> DetectedMods = [];

        public Compat(BepInEx.PluginInfo[] plugins)
        {
            foreach (var plugin in plugins)
            {
                var mod = ModCompatibilityList.FirstOrDefault((mod) => mod[1] == plugin.Metadata.GUID);

                if (mod == null)
                    continue;

                if (mod[2] != null && plugin.Metadata.Version.ToString() != mod[2] && !Plugin.Config.OverrideCompatibilityVersionCheck.Value)
                    continue;

                Logger.LogInfo($"Found compatible mod {mod[0]}");

                DetectedMods.Add(mod[0]);
            }
        }

        public bool IsLoaded(string modName)
        {
            return DetectedMods.Contains(modName);
        }
    }
}
