using System.Runtime.InteropServices;
using System.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Security;

namespace LCVR;

internal static class Native
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder strText, int maxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern void AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentProcessId();

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    public static readonly IntPtr HKEY_LOCAL_MACHINE = new(0x80000002);

    [DllImport("Advapi32.dll", EntryPoint = "RegOpenKeyExA", CharSet = CharSet.Ansi)]
    public static extern int RegOpenKeyEx(IntPtr hKey, [In] string lpSubKey, int ulOptions, int samDesired,
        out IntPtr phkResult);

    [DllImport("advapi32.dll", CharSet = CharSet.Ansi)]
    public static extern int RegQueryValueEx(IntPtr hKey, string lpValueName, int lpReserved, out uint lpType,
        StringBuilder lpData, ref uint lpcbData);

    [DllImport("advapi32.dll", CharSet = CharSet.Ansi)]
    public static extern int RegQueryInfoKey(IntPtr hKey, StringBuilder lpClass, IntPtr lpcbClass, IntPtr lpReserved,
        out uint lpcSubKeys, out uint lpcbMaxSubKeyLen, out uint lpcbMaxClassLen, out uint lpcValues,
        out uint lpcbMaxValueNameLen, out uint lpcbMaxValueLen, IntPtr lpSecurityDescriptor, IntPtr lpftLastWriteTime);

    [DllImport("advapi32.dll", EntryPoint = "RegEnumValueA", CharSet = CharSet.Ansi)]
    public static extern int RegEnumValue(IntPtr hKey, uint dwIndex, StringBuilder lpValueName, ref uint lpcchValueName,
        IntPtr lpReserved, IntPtr lpType, IntPtr lpData, IntPtr lpcbData);

    [DllImport("advapi32.dll")]
    public static extern int RegCloseKey(IntPtr hKey);

    [DllImport("Shlwapi.dll", CharSet = CharSet.Ansi)]
    public static extern int ShellMessageBox(IntPtr hAppInst, IntPtr hWnd, string lpcText, string lpcTitle,
        uint fuStyle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool OpenProcessToken(IntPtr hProcess, uint dwAccess, out IntPtr hToken);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool GetTokenInformation(IntPtr hToken, uint tokenInformationClass, IntPtr lpData,
        uint tokenInformationLength, out uint returnLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
    [SuppressUnmanagedCodeSecurity]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);

    public static bool RegOpenSubKey(ref IntPtr hKey, string lpSubKey, int samDesired)
    {
        var result = RegOpenKeyEx(hKey, lpSubKey, 0, samDesired, out var hNewKey) == 0;
        if (!result)
            return false;

        RegCloseKey(hKey);
        hKey = hNewKey;

        return true;
    }

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    private static string GetWindowText(IntPtr hWnd)
    {
        int size = GetWindowTextLength(hWnd);
        if (size > 0)
        {
            // Include space for termination char
            var builder = new StringBuilder(size + 1);
            GetWindowText(hWnd, builder, builder.Capacity);
            return builder.ToString();
        }

        return string.Empty;
    }

    private static IEnumerable<IntPtr> FindWindows(EnumWindowsProc filter)
    {
        IntPtr found = IntPtr.Zero;
        List<IntPtr> windows = [];

        EnumWindows(delegate(IntPtr wnd, IntPtr param)
        {
            if (filter(wnd, param))
            {
                windows.Add(wnd);
            }

            return true;
        }, IntPtr.Zero);

        return windows;
    }

    public static void BringGameWindowToFront()
    {
        var currentPid = GetCurrentProcessId();

        var gameWindows = FindWindows(delegate(IntPtr hWnd, IntPtr lParam)
        {
            GetWindowThreadProcessId(hWnd, out var pid);

            if (pid != currentPid)
                return false;

            return GetWindowText(hWnd) == "Lethal Company";
        }).ToArray();

        if (gameWindows.Length > 1)
            Logger.LogWarning("Multiple game windows called 'Lethal Company' detected. Selecting only the first one.");

        var targetWindow = gameWindows[0];

        // Little hack to make BringWindowToTop work properly
        var foregroundPid = GetWindowThreadProcessId(GetForegroundWindow(), out _);
        var currentThreadId = GetCurrentThreadId();

        AttachThreadInput(foregroundPid, currentThreadId, true);
        BringWindowToTop(targetWindow);
        AttachThreadInput(foregroundPid, currentThreadId, false);
    }

    public static bool IsElevated()
    {
        var hToken = IntPtr.Zero;
        var data = IntPtr.Zero;

        try
        {
            if (!OpenProcessToken(GetCurrentProcess(), 0x0008, out hToken))
                return false;

            data = Marshal.AllocHGlobal(4);
            if (!GetTokenInformation(hToken, 20, data, 4, out _))
                return false;

            return Marshal.ReadIntPtr(data).ToInt32() != 0;
        }
        finally
        {
            if (hToken != IntPtr.Zero)
                CloseHandle(hToken);

            if (data != IntPtr.Zero)
                Marshal.FreeHGlobal(data);
        }
    }
}
