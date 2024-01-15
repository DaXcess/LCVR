using System.Runtime.InteropServices;
using System.Text;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LCVR
{
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

            EnumWindows(delegate (IntPtr wnd, IntPtr param)
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

            var gameWindows = FindWindows(delegate (IntPtr hWnd, IntPtr lParam)
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
    }
}
