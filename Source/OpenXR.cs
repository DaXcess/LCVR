using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using BepInEx.Logging;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using Steamworks;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;

namespace LCVR;

internal static class OpenXR
{
    [DllImport("UnityOpenXR", EntryPoint = "DiagnosticReport_GenerateReport")]
    private static extern IntPtr Internal_GenerateReport();

    [DllImport("UnityOpenXR", EntryPoint = "DiagnosticReport_ReleaseReport")]
    private static extern void Internal_ReleaseReport(IntPtr report);
    
    [DllImport("UnityOpenXR", EntryPoint = "NativeConfig_GetRuntimeName")]
    private static extern bool Internal_GetRuntimeName(out IntPtr runtimeNamePtr);

    [DllImport("UnityOpenXR", EntryPoint = "NativeConfig_GetRuntimeVersion")]
    private static extern bool Internal_GetRuntimeVersion(out ushort major, out ushort minor, out ushort patch);

    /// <summary>
    /// Generate an OpenXR diagnostics report
    /// </summary>
    private static string GenerateReport()
    {
        var handle = Internal_GenerateReport();
        if (handle == IntPtr.Zero)
            return "";

        var result = Marshal.PtrToStringAnsi(handle);
        Internal_ReleaseReport(handle);

        return result;
    }
    
    /// <summary>
    /// Attempt to enumerate installed OpenXR runtimes as described by the <a href="https://registry.khronos.org/OpenXR/specs/1.1/loader.html#runtime-discovery">OpenXR standard</a>.
    /// </summary>
    public static Runtimes GetRuntimes()
    {
        var runtimes = new HashSet<Runtime>();

        foreach (var rt in LocateCommonRuntimes())
            runtimes.Add(rt);

        var defaultPath = GetDefaultRuntimePath();

        if (Native.RegOpenKeyEx(Native.HKEY_LOCAL_MACHINE, "SOFTWARE\\Khronos\\OpenXR\\1", 0, 0x20019, out var hKey) !=
            0)
            return new Runtimes(runtimes.ToArray());

        var files = new List<string>();
        if (!Native.RegOpenSubKey(ref hKey, "AvailableRuntimes", 0x20019) || !EnumRuntimeFiles(hKey, files))
        {
            // Only return the default runtime

            if (string.IsNullOrEmpty(defaultPath))
                return new Runtimes(runtimes.ToArray());

            try
            {
                var runtimeInfo =
                    JsonConvert.DeserializeObject<JToken>(File.ReadAllText(defaultPath))["runtime"];

                runtimes.Add(
                    new Runtime
                    {
                        Name = runtimeInfo?["name"]?.ToObject<string>(),
                        Path = defaultPath,
                        Default = true
                    }
                );
            }
            catch
            {
                // ignored
            }

            return new Runtimes(runtimes.ToArray());
        }

        if (!files.Contains(defaultPath))
            files.Add(defaultPath);

        foreach (var file in files)
        {
            try
            {
                var runtimeInfo = JsonConvert.DeserializeObject<JToken>(File.ReadAllText(file))["runtime"];

                runtimes.Add(new Runtime
                {
                    Name = runtimeInfo?["name"]?.ToObject<string>(),
                    Path = file,
                    Default = file == defaultPath
                });
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Failed to parse {file}: {ex.Message}. Runtime will not be used.");
            }
        }

        return new Runtimes(runtimes.ToArray());
    }

    [CanBeNull]
    private static string GetDefaultRuntimePath()
    {
        if (Native.RegOpenKeyEx(Native.HKEY_LOCAL_MACHINE, "SOFTWARE\\Khronos\\OpenXR\\1", 0, 0x20019, out var hKey) !=
            0)
            return null;

        string defaultRuntimePath = null;

        var cbData = 0u;
        if (Native.RegQueryValueEx(hKey, "ActiveRuntime", 0, out var type, null, ref cbData) != 0 ||
            type is not (0x1 or 0x2)) return null;

        var data = new StringBuilder((int)cbData);
        if (Native.RegQueryValueEx(hKey, "ActiveRuntime", 0, out type, data, ref cbData) == 0)
            defaultRuntimePath = data.ToString();

        return defaultRuntimePath;
    }

    private static bool EnumRuntimeFiles(IntPtr hKey, List<string> files)
    {
        if (Native.RegQueryInfoKey(hKey, null, IntPtr.Zero, IntPtr.Zero, out _, out _, out _, out var valueCount,
                out var maxValueNameLength, out _, IntPtr.Zero, IntPtr.Zero) != 0)
            return false;

        for (uint i = 0; i < valueCount; i++)
        {
            var valueName = new StringBuilder((int)maxValueNameLength + 1);
            var cbValueName = maxValueNameLength + 1;

            if (Native.RegEnumValue(hKey, i, valueName, ref cbValueName, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
                    IntPtr.Zero) != 0)
                continue;

            files.Add(valueName.ToString());
        }

        return true;
    }
    
    /// <summary>
    /// Locate common paths for runtimes
    /// </summary>
    private static IEnumerable<Runtime> LocateCommonRuntimes()
    {
        // 1. SteamVR
        var runtime = Utils.ExecuteWithSteamAPI<Runtime?>(() =>
        {
            if (SteamApps.IsAppInstalled(250820) && SteamApps.AppInstallDir(250820) is { } path &&
                Runtime.ReadFromJson(Path.Combine(path, "steamxr_win64.json"), out var runtime))
                return runtime;

            return null;
        });

        if (runtime != null)
        {
            yield return runtime.Value;
        }

        // 2. Virtual Desktop
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            @"Virtual Desktop Streamer\OpenXR\virtualdesktop-openxr.json");

        if (File.Exists(path) && Runtime.ReadFromJson(path, out var rt))
            yield return rt;

        // 3. Oculus
        path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            @"Oculus\Support\oculus-runtime\oculus_openxr_64.json");

        if (File.Exists(path) && Runtime.ReadFromJson(path, out rt))
            yield return rt;
    }

    public static bool GetActiveRuntimeName(out string name)
    {
        name = null;

        if (!Internal_GetRuntimeName(out var ptr))
            return false;

        if (ptr == IntPtr.Zero)
            return false;

        name = Marshal.PtrToStringAnsi(ptr);

        return true;
    }

    public static bool GetActiveRuntimeVersion(out ushort major, out ushort minor, out ushort patch)
    {
        return Internal_GetRuntimeVersion(out major, out minor, out patch);
    }

    public class Runtimes(Runtime[] runtimes) : IReadOnlyCollection<Runtime>
    {
        public Runtime? Default => runtimes.Select(rt => (Runtime?)rt).FirstOrDefault(rt => rt.Value.Default);
        public int Count => runtimes.Length;

        public bool TryGetRuntime(string name, out Runtime runtime)
        {
            runtime = default;

            try
            {
                runtime = runtimes.First(rt => rt.Name == name);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool TryGetRuntimeByPath(string path, out Runtime runtime)
        {
            runtime = default;

            try
            {
                runtime = runtimes.First(rt => rt.Path == path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public IEnumerator<Runtime> GetEnumerator()
        {
            // ReSharper disable once NotDisposedResourceIsReturned
            return ((IEnumerable<Runtime>)runtimes).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public struct Runtime : IEquatable<Runtime>
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public bool Default { get; set; }
        
        public static bool ReadFromJson(string path, out Runtime runtime)
        {
            runtime = new Runtime();

            try
            {
                var runtimeInfo = JsonConvert.DeserializeObject<JToken>(File.ReadAllText(path))["runtime"];

                runtime.Name = runtimeInfo?["name"]?.ToObject<string>();
                runtime.Path = path;

                if (GetDefaultRuntimePath() is { } defaultPath)
                    runtime.Default = string.Equals(path, defaultPath, StringComparison.CurrentCultureIgnoreCase);

                return true;
            }
            catch
            {
                return false;
            }
        }
        
        public bool Equals(Runtime other)
        {
            return Path == other.Path;
        }

        public override bool Equals(object obj)
        {
            return obj is Runtime other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Path);
        }
    }

    public static class Loader
    {
        private static XRGeneralSettings xrGeneralSettings;
        private static XRManagerSettings xrManagerSettings;
        private static OpenXRLoader xrLoader;

        private static readonly ManualLogSource Logger = new("OpenXR Loader");

        static Loader()
        {
            BepInEx.Logging.Logger.Sources.Add(Logger);
        }

        public static bool InitializeXR()
        {
            InitializeScripts();

            if (Native.IsElevated())
            {
                Logger.LogWarning(
                    "Application is elevated! Unable to override the XR runtime! Only the system default OpenXR runtime will be available.");

                return InitializeXR(null);
            }

            if (GetRuntimes() is var runtimes && runtimes.Count == 0)
            {
                Logger.LogWarning("Failed to query runtimes, or no runtimes were found. Falling back to old behavior.");

                // On failure, revert back to pre 1.2.4 behavior (Default runtime or the one specified by the config)
                return InitializeXR(string.IsNullOrEmpty(Plugin.Config.OpenXRRuntimeFile.Value)
                    ? null
                    : new Runtime
                    {
                        Name = "LCVR OpenXR Override",
                        Path = Plugin.Config.OpenXRRuntimeFile.Value
                    });
            }

            if (!string.IsNullOrEmpty(Plugin.Config.OpenXRRuntimeFile.Value))
            {
                var rtFound = runtimes.TryGetRuntimeByPath(Plugin.Config.OpenXRRuntimeFile.Value, out var rt);

                if (InitializeXR(rtFound
                        ? rt
                        : new Runtime
                        {
                            Name = "LCVR OpenXR Override",
                            Path = Plugin.Config.OpenXRRuntimeFile.Value
                        }))
                    return true;

                Logger.LogWarning("Loading OpenXR using override failed, falling back to automatic enumeration...");
            }

            // Make sure the default runtime is first (unless it's the override which already failed at this point)
            if (runtimes.Default is { } @default && @default.Path != Plugin.Config.OpenXRRuntimeFile.Value)
            {
                if (InitializeXR(@default))
                    return true;
            }

            foreach (var runtime in runtimes.Where(
                         rt => rt.Path != Plugin.Config.OpenXRRuntimeFile.Value && !rt.Default))
            {
                if (InitializeXR(runtime))
                    return true;
            }

            Logger.LogError("All available runtimes were attempted but none worked. Aborting...");
            return false;
        }

        private static bool InitializeXR(Runtime? runtime)
        {
            if (runtime is { } rt)
            {
                Logger.LogInfo($"Attempting to initialize OpenXR on {rt.Name}");
                Environment.SetEnvironmentVariable("XR_RUNTIME_JSON", rt.Path);
            }
            else
            {
                Logger.LogInfo("Attempting to initialize OpenXR using default runtime");
                Environment.SetEnvironmentVariable("XR_RUNTIME_JSON", null);
            }

            xrGeneralSettings.InitXRSDK();
            xrGeneralSettings.Start();

            var displays = new List<XRDisplaySubsystem>();
            SubsystemManager.GetInstances(displays);

            if (Plugin.Config.EnableVerboseLogging.Value)
            {
                Logger.LogWarning("OpenXR Diagnostics Report:");

                foreach (var line in GenerateReport().Split("\n"))
                    Logger.LogWarning(line);

                Logger.LogWarning("");
                Logger.LogWarning(
                    "To prevent diagnostics report from being printed, disable the 'EnableVerboseLogging' option in the settings.");
            }

            return displays.Count > 0;
        }

        private static void InitializeScripts()
        {
            xrGeneralSettings ??= ScriptableObject.CreateInstance<XRGeneralSettings>();
            xrManagerSettings ??= ScriptableObject.CreateInstance<XRManagerSettings>();
            xrLoader ??= ScriptableObject.CreateInstance<OpenXRLoader>();

            xrGeneralSettings.Manager = xrManagerSettings;

            ((List<XRLoader>)xrManagerSettings.activeLoaders).Clear();
            ((List<XRLoader>)xrManagerSettings.activeLoaders).Add(xrLoader);

            OpenXRSettings.Instance.renderMode = OpenXRSettings.RenderMode.MultiPass;
            OpenXRSettings.Instance.depthSubmissionMode = OpenXRSettings.DepthSubmissionMode.None;

            if (OpenXRSettings.Instance.features.Length != 0)
                return;

            var valveIndex = ScriptableObject.CreateInstance<ValveIndexControllerProfile>();
            var hpReverb = ScriptableObject.CreateInstance<HPReverbG2ControllerProfile>();
            var htcVive = ScriptableObject.CreateInstance<HTCViveControllerProfile>();
            var mmController = ScriptableObject.CreateInstance<MicrosoftMotionControllerProfile>();
            var khrSimple = ScriptableObject.CreateInstance<KHRSimpleControllerProfile>();
            var metaQuestTouch = ScriptableObject.CreateInstance<MetaQuestTouchProControllerProfile>();
            var oculusTouch = ScriptableObject.CreateInstance<OculusTouchControllerProfile>();

            valveIndex.enabled = true;
            hpReverb.enabled = true;
            htcVive.enabled = true;
            mmController.enabled = true;
            khrSimple.enabled = true;
            metaQuestTouch.enabled = true;
            oculusTouch.enabled = true;

            OpenXRSettings.Instance.features =
            [
                valveIndex,
                hpReverb,
                htcVive,
                mmController,
                khrSimple,
                metaQuestTouch,
                oculusTouch
            ];
        }
    }
}
