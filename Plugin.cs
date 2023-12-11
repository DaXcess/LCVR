using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;

namespace LethalCompanyVR
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    //[BepInIncompatibility("com.sinai.unityexplorer")]
    public class Plugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "io.daxcess.lcvr";
        public const string PLUGIN_NAME = "LCVR";
        public const string PLUGIN_VERSION = "0.0.1";

        private void Awake()
        {
            // Plugin startup logic
            LethalCompanyVR.Logger.SetSource(Logger);

            Logger.LogInfo($"Plugin {PLUGIN_NAME} is starting...");

            // Allow disabling VR via command line
            if (Environment.GetCommandLineArgs().Contains("--disable-vr", StringComparer.OrdinalIgnoreCase))
            {
                Logger.LogWarning("VR has been disabled by the `--disable-vr` command line flag");
                return;
            }

            if (!AssetManager.LoadAssets())
            {
                Logger.LogError("Disabling VR because assets could not be loaded!");
                return;
            }

            // TODO: Make this configurable
            var asset = QualitySettings.renderPipeline as HDRenderPipelineAsset;
            var settings = asset.currentPlatformRenderPipelineSettings;

            settings.dynamicResolutionSettings.enabled = true;
            settings.dynamicResolutionSettings.enableDLSS = false;
            settings.dynamicResolutionSettings.dynResType = DynamicResolutionType.Hardware;
            settings.dynamicResolutionSettings.upsampleFilter = DynamicResUpscaleFilter.CatmullRom;
            settings.dynamicResolutionSettings.minPercentage = 25;
            settings.dynamicResolutionSettings.maxPercentage = 50;

            asset.currentPlatformRenderPipelineSettings = settings;
            
            Logger.LogInfo("Loading VR...");
            InitVRLoader();
        }

        private void InitVRLoader()
        {
            EnableControllerProfiles();
            InitializeXRRuntime();

            if (!StartDisplay())
            {
                Logger.LogError("Failed to start in VR Mode");

                return;
            }

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

            Logger.LogDebug("Inserted VR patches using Harmony");
        }

        /// <summary>
        /// Loads controller profiles provided by Unity into OpenXR, which will enable controller support.
        /// By default, only the HMD input profile is loaded.
        /// </summary>
        private void EnableControllerProfiles()
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

            Logger.LogDebug("Enabled XR Controller Profiles");
        }

        /// <summary>
        /// Attempt to start the OpenXR runtime.
        /// </summary>
        private void InitializeXRRuntime()
        {
            // Set up the OpenXR loader
            var generalSettings = ScriptableObject.CreateInstance<XRGeneralSettings>();
            var managerSettings = ScriptableObject.CreateInstance<XRManagerSettings>();
            var xrLoader = ScriptableObject.CreateInstance<OpenXRLoader>();

            generalSettings.Manager = managerSettings;

            // Casting this, because I couldn't stand the `this field is obsolete` warning
            ((List<XRLoader>)managerSettings.activeLoaders).Clear();
            ((List<XRLoader>)managerSettings.activeLoaders).Add(xrLoader);

            OpenXRSettings.Instance.renderMode = OpenXRSettings.RenderMode.MultiPass;

            typeof(XRGeneralSettings).GetMethod("InitXRSDK", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(generalSettings, []);
            typeof(XRGeneralSettings).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(generalSettings, []);

            Logger.LogDebug("Initialized XR Runtime");
        }

        /// <summary>
        /// Start the display subsystem (I have no idea what that means).
        /// </summary>
        /// <returns><see langword="false"/> if no displays were found, <see langword="true"/> otherwise.</returns>
        private bool StartDisplay()
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
