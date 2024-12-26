using BepInEx.Bootstrap;

namespace LCVR;

public static class Compat
{
    public const string MoreCompany = "me.swipez.melonloader.morecompany";
    public const string CullFactory = "com.fumiko.CullFactory";

    public static bool IsLoaded(string modId)
    {
        return Chainloader.PluginInfos.ContainsKey(modId);
    }
}
