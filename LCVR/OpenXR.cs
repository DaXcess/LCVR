using System;
using System.Runtime.InteropServices;

namespace LCVR
{
    internal class OpenXR
    {
        [DllImport("UnityOpenXR", EntryPoint = "DiagnosticReport_GenerateReport")]
        private static extern IntPtr Internal_GenerateReport();

        [DllImport("UnityOpenXR", EntryPoint = "DiagnosticReport_ReleaseReport")]
        private static extern void Internal_ReleaseReport(IntPtr report);

        internal static string GenerateReport()
        {
            string result = "";
            IntPtr intPtr = Internal_GenerateReport();
            if (intPtr != IntPtr.Zero)
            {
                result = Marshal.PtrToStringAnsi(intPtr);
                Internal_ReleaseReport(intPtr);
                intPtr = IntPtr.Zero;
            }

            return result;
        }

        public static void DumpOpenXRDiag()
        {
            Logger.LogWarning(GenerateReport());
        }
    }
}
