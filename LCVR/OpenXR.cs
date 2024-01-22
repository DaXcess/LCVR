using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace LCVR
{
    internal class OpenXR
    {
        [DllImport("UnityOpenXR", EntryPoint = "DiagnosticReport_GenerateReport")]
        private static extern IntPtr Internal_GenerateReport();

        [DllImport("UnityOpenXR", EntryPoint = "DiagnosticReport_ReleaseReport")]
        private static extern void Internal_ReleaseReport(IntPtr report);

        [DllImport("UnityOpenXR", EntryPoint = "NativeConfig_GetRuntimeName")]
        private static extern bool Internal_GetRuntimeName(out IntPtr runtimeNamePtr);

        [DllImport("UnityOpenXR", EntryPoint = "NativeConfig_GetRuntimeVersion")]
        private static extern bool Internal_GetRuntimeVersion(out ushort major, out ushort minor, out ushort patch);

        public static string GenerateReport()
        {
            string result = "";
            IntPtr intPtr = Internal_GenerateReport();
            if (intPtr != IntPtr.Zero)
            {
                result = Marshal.PtrToStringAnsi(intPtr);
                Internal_ReleaseReport(intPtr);
            }

            return result;
        }

        public static bool GetDiagnosticReport(out OpenXRReport report)
        {
            report = null;

            var sectionRegex = new Regex("^==== ([A-z0-9-_ ]+) ====$", RegexOptions.Multiline);
            var errorRegex = new Regex("^\\[FAILURE\\] [A-z]+: ([A-Z_]+) \\(\\d+x\\)$");

            string raw = GenerateReport();

            var rawSections = sectionRegex.Split(raw).Skip(1).Select(v => v.Trim()).ToArray();
            var sections = new Dictionary<string, string>();

            for (var i = 0; i < rawSections.Length; i += 2)
                sections.Add(rawSections[i], rawSections[i + 1]);

            if (!sections.TryGetValue("OpenXR Runtime Info", out string section))
                return false;

            var lines = section.Split('\n');

            var runtimeName = lines.FirstOrDefault(line => line.StartsWith("Runtime Name: "));
            var runtimeVersion = lines.FirstOrDefault(line => line.StartsWith("Runtime Version: "));

            if (runtimeName == default || runtimeVersion == default)
            {
                runtimeName = "<Missing>";
                runtimeVersion = "<Missing>";
            }
            else
            {
                runtimeName = runtimeName.Split(": ")[1];
                runtimeVersion = runtimeVersion.Split(": ")[1];
            }

            if (!sections.TryGetValue("Last 20 non-XR_SUCCESS returns", out section))
                return false;

            var match = errorRegex.Match(section.Split('\n')[0].Trim());
            if (match == null)
                return false;

            var error = match.Groups[1].Value;

            report = new OpenXRReport(runtimeName, runtimeVersion, error);

            return true;
        }

        public static string[] DetectOpenXRRuntimes()
        {
            var hKey = IntPtr.Zero;
            var cbData = 0u;

            try
            {
                if (Native.RegOpenKeyEx(Native.HKEY_LOCAL_MACHINE, "SOFTWARE\\Khronos\\OpenXR\\1", 0, 0x20019, out hKey) != 0)
                    return null;

                if (Native.RegQueryValueEx(hKey, "ActiveRuntime", 0, out var type, null, ref cbData) != 0)
                    return null;

                var data = new StringBuilder((int)cbData);

                if (Native.RegQueryValueEx(hKey, "ActiveRuntime", 0, out type, data, ref cbData) != 0)
                    return null;

                var path = data.ToString();
                var defaultRuntime = JsonConvert.DeserializeObject<OpenXRRuntime>(File.ReadAllText(path)).runtime.name;

                if (Native.RegOpenKeyEx(hKey, "AvailableRuntimes", 0, 0x20019, out hKey) != 0)
                    return null;

                if (Native.RegQueryInfoKey(hKey, null, IntPtr.Zero, IntPtr.Zero, out _, out _, out _, out var valueCount, out var maxValueNameLength, out _, IntPtr.Zero, IntPtr.Zero) != 0)
                    return null;

                var values = new List<string>();

                for (uint i = 0; i < valueCount; i++)
                {
                    var valueName = new StringBuilder((int)maxValueNameLength + 1);
                    var cbValueName = maxValueNameLength + 1;

                    int result = Native.RegEnumValue(hKey, i, valueName, ref cbValueName, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

                    if (result != 0)
                        return null;

                    values.Add(valueName.ToString());
                }

                var runtimes = values.Select(value => JsonConvert.DeserializeObject<OpenXRRuntime>(File.ReadAllText(value)).runtime.name).ToArray();

                Array.Sort(runtimes, (a, b) =>
                {
                    if (a == defaultRuntime) return -1;
                    if (b == defaultRuntime) return 1;
                    return string.Compare(a, b, StringComparison.Ordinal);
                });

                return runtimes;
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Failed to query runtimes: {ex.Message}");
                return null;
            }
            finally
            {
                if (hKey != IntPtr.Zero)
                    Native.RegCloseKey(hKey);
            }
        }

        public static bool GetRuntimeName(out string name)
        {
            name = null;

            if (!Internal_GetRuntimeName(out var ptr))
                return false;

            if (ptr == IntPtr.Zero)
                return false;

            name = Marshal.PtrToStringAnsi(ptr);

            return true;
        }

        public static bool GetRuntimeVersion(out ushort major, out ushort minor, out ushort patch)
        {
            return Internal_GetRuntimeVersion(out major, out minor, out patch);
        }

        public class OpenXRReport(string runtimeName, string runtimeVersion, string error)
        {
            public string RuntimeName { get; } = runtimeName;
            public string RuntimeVersion { get; } = runtimeVersion;
            public string Error { get; } = error;
        }

#pragma warning disable 0649
        [Serializable]
        private struct OpenXRRuntime
        {
            public RuntimeInfo runtime;
        }

        [Serializable]
        private struct RuntimeInfo
        {
            public string name;
        }
#pragma warning restore 0649
    }
}
