using BepInEx;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;

namespace LethalCompanyVR
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    //[BepInIncompatibility("com.sinai.unityexplorer")]
    public class Plugin : BaseUnityPlugin
    {
        public static bool VR_ENABLED = true;

        /// <summary>
        /// Temporary value to tell the game to instantly host a game
        /// </summary>
        public static bool FORCE_INGAME = true;

        /// <summary>
        /// The main in-game player camera
        /// </summary>
        public static Camera MainCamera = null;

        /// <summary>
        /// The main render camera. This is the camera that actually outputs to the HMD/Monitor.
        /// </summary>
        public static Camera RenderCamera
        {
            get => GameObject.Find("UICamera").GetComponent<Camera>();
        }

        private void Awake()
        {
            // Plugin startup logic
            LethalCompanyVR.Logger.SetSource(Logger);

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is starting...");

            // Allow disabling VR via command line
            if (Environment.GetCommandLineArgs().Contains("--disable-vr", StringComparer.OrdinalIgnoreCase))
            {
                Logger.LogWarning("VR has been disabled by the `--disable-vr` command line flag");
                return;
            }

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

            Logger.LogDebug("Inserted VR patches using Harmony");
            
            Logger.LogInfo("Loading VR...");
            StartCoroutine(InitVRLoader());
        }

        private IEnumerator InitVRLoader()
        {
            EnableControllerProfiles();
            InitializeXRRuntime();

            if (!StartDisplay())
            {
                Logger.LogError("Failed to start in VR Mode, disabling VR...");

                VR_ENABLED = false;

                yield break;
            }

            var devices = new List<InputDevice>();
            InputDevices.GetDevices(devices);

            foreach (InputDevice device in devices)
            {
                Logger.LogDebug(device.name);
            }

            yield break;
        }

        /// <summary>
        /// Loads controller profiles provided by Unity into OpenXR, which will enable controller support.
        /// By default, only the HMD input profile is loaded.
        /// </summary>
        private static void EnableControllerProfiles()
        {
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
            // This feature list is empty by default if the game isn't a VR game

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

            LethalCompanyVR.Logger.LogDebug("Enabled XR Controller Profiles");
        }

        /// <summary>
        /// Attempt to start the OpenXR runtime.
        /// </summary>
        private static void InitializeXRRuntime()
        {
            // Set up the OpenXR loader
            var generalSettings = ScriptableObject.CreateInstance<XRGeneralSettings>();
            var managerSettings = ScriptableObject.CreateInstance<XRManagerSettings>();
            var xrLoader = ScriptableObject.CreateInstance<OpenXRLoader>();

            generalSettings.Manager = managerSettings;

            // Casting this, because I couldn't stand the `this field is obsolete` warning
            ((List<XRLoader>)managerSettings.activeLoaders).Clear();
            ((List<XRLoader>)managerSettings.activeLoaders).Add(xrLoader);

            // TODO: Test if this is even necessary
            OpenXRSettings.Instance.renderMode = OpenXRSettings.RenderMode.MultiPass;

            managerSettings.InitializeLoaderSync();

            // TODO: Ditto for these
            typeof(XRGeneralSettings).GetMethod("AttemptInitializeXRSDKOnLoad", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, []);
            typeof(XRGeneralSettings).GetMethod("AttemptStartXRSDKOnBeforeSplashScreen", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, []);

            LethalCompanyVR.Logger.LogDebug("Initialized XR Runtime");
        }

        /// <summary>
        /// Start the display subsystem (I have no idea what that means).
        /// </summary>
        /// <returns><see langword="false"/> if no displays were found, <see langword="true"/> otherwise.</returns>
        private static bool StartDisplay()
        {
            var displays = new List<XRDisplaySubsystem>();

            SubsystemManager.GetInstances(displays);

            if (displays.Count < 1)
            {
                return false;
            }

            displays[0].Start();

            return true;
        }
    }
}
