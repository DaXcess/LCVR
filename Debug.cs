using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace LethalCompanyVR
{
    internal class Debug
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
