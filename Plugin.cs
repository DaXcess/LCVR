using BepInEx;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;

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

        // TODO: Do I need to add controller bindings here???
        static IEnumerator InitVRLoader()
        {
            var generalSettings = ScriptableObject.CreateInstance<XRGeneralSettings>();
            managerSettings = ScriptableObject.CreateInstance<XRManagerSettings>();
            var xrLoader = ScriptableObject.CreateInstance<OpenXRLoader>();

            OpenXRSettings.Instance.renderMode = OpenXRSettings.RenderMode.MultiPass;

            generalSettings.Manager = managerSettings;

            managerSettings.loaders.Clear();
            managerSettings.loaders.Add(xrLoader);

            managerSettings.InitializeLoaderSync();

            typeof(XRGeneralSettings).GetMethod("AttemptInitializeXRSDKOnLoad", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, []);
            typeof(XRGeneralSettings).GetMethod("AttemptStartXRSDKOnBeforeSplashScreen", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, []);

            SubsystemManager.GetInstances(displays);

            myDisplay = displays[0];
            myDisplay.Start();

            yield return null;
        }
    }
}
