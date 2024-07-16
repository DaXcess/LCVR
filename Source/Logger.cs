using BepInEx.Logging;

namespace LCVR;

public static class Logger
{
    private static ManualLogSource logSource;

    public static void SetSource(ManualLogSource Logger)
    {
        logSource = Logger;
    }

    public static void Log(object message)
    {
        logSource.LogInfo(message);
    }

    public static void LogInfo(object message)
    {
        logSource.LogInfo(message);
    }

    public static void LogWarning(object message)
    {
        logSource.LogWarning(message);
    }

    public static void LogError(object message)
    {
        logSource.LogError(message);
    }

    public static void LogDebug(object message)
    {
        logSource.LogDebug(message);
    }
}