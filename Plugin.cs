using BepInEx;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;

namespace LethalCompanyVR
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static bool VR_ENABLED = true;

        public static XRManagerSettings managerSettings = null;

        public static List<XRDisplaySubsystemDescriptor> displayDescriptors = new List<XRDisplaySubsystemDescriptor>();
        public static List<XRDisplaySubsystem> displays = new List<XRDisplaySubsystem>();
        public static XRDisplaySubsystem myDisplay = null;

        public static GameObject secondEye = null;
        public static Camera secondCam = null;

        public class MyStaticMB : MonoBehaviour { }
        public static MyStaticMB myStaticMB;

        public static Plugin Instance = null;

        private void Awake()
        {
            Instance = this;
            LethalCompanyVR.Logger.SetSource(Logger);

            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is starting...");
                        
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

            if (myStaticMB == null)
            {
                var @object = new GameObject("MyStatic");

                myStaticMB = @object.AddComponent<MyStaticMB>();
            }

            if (VR_ENABLED) myStaticMB.StartCoroutine(InitVRLoader());
        }

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

        // TODO: Clean clean clean
        private IEnumerator InitVRLoader()
        {
            var generalSettings = ScriptableObject.CreateInstance<XRGeneralSettings>();
            managerSettings = ScriptableObject.CreateInstance<XRManagerSettings>();
            var xrLoader = ScriptableObject.CreateInstance<OpenXRLoader>();

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
            
            // Patch the OpenXRSettings.features field to include controller profiles
            OpenXRFeature[] features = (OpenXRFeature[])typeof(OpenXRSettings).GetField("features", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(OpenXRSettings.Instance);
            var featList = new List<OpenXRFeature>()
            {
                valveIndex,
                hpReverb,
                htcVive,
                mmController,
                khrSimple,
                metaQuestTouch,
                oculusTouch
            };
            typeof(OpenXRSettings).GetField("features", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(OpenXRSettings.Instance, featList.ToArray());

            OpenXRSettings.Instance.renderMode = OpenXRSettings.RenderMode.MultiPass;

            generalSettings.Manager = managerSettings;

            managerSettings.loaders.Clear();
            managerSettings.loaders.Add(xrLoader);

            managerSettings.InitializeLoaderSync();

            typeof(XRGeneralSettings).GetMethod("AttemptInitializeXRSDKOnLoad", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, []);
            typeof(XRGeneralSettings).GetMethod("AttemptStartXRSDKOnBeforeSplashScreen", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, []);

            SubsystemManager.GetInstances(displays);

            if (displays.Count < 1)
            {
                VR_ENABLED = false;
            }

            myDisplay = displays[0];
            myDisplay.Start();

            Logger.LogWarning(GenerateReport());

            yield return null;
        }
    }
}
