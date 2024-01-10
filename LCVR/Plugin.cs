﻿using BepInEx;
using BepInEx.Bootstrap;
using GameNetcodeStuff;
using LCVR.Assets;
using LCVR.Patches;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;

namespace LCVR
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInDependency("me.swipez.melonloader.morecompany", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "io.daxcess.lcvr";
        public const string PLUGIN_NAME = "LCVR";
        public const string PLUGIN_VERSION = "1.0.0";

        private readonly string[] GAME_ASSEMBLY_HASHES = [
            "AAC6149C355A19865C0F67FD0C1D7111D4F418EF94D700265B591665B4CDCE73", // V45
            "9F7F9C7F7159628992127770CA4E294F2313CD1FB0713BFC3FD9274BA018EEC7"  // V47 -Public beta
        ];

        public static new Config Config { get; private set; }
        public static Compat Compatibility { get; private set; }
        public static Flags Flags { get; private set; }

        private void Awake()
        {
            // Reload Unity's Input System plugins since BepInEx in some
            // configurations runs after the Input System has already been initialized
            typeof(InputSystem).GetMethod("PerformDefaultPluginInitialization", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, []);

            // Plugin startup logic
            LCVR.Logger.SetSource(Logger);

            Config = new Config(base.Config);
            Compatibility = new Compat([.. Chainloader.PluginInfos.Values]);

            foreach (var plugin in Chainloader.PluginInfos.Values)
                if (plugin.Metadata.GUID == "com.sinai.unityexplorer")
                {
                    Logger.LogWarning("WARNING: UNITY EXPLORER DETECTED! UNITY EXPLORER *WILL* BREAK VR UI INPUTS!");
                    Flags |= Flags.UnityExplorerDetected;
                }

            Logger.LogInfo($"Plugin {PLUGIN_NAME} is starting...");

            // Allow disabling VR via config and command line
            var disableVr = Config.DisableVR.Value ||
                            Environment.GetCommandLineArgs().Contains("--disable-vr", StringComparer.OrdinalIgnoreCase);

            if (disableVr)
                Logger.LogWarning("VR has been disabled by config or the `--disable-vr` command line flag");

            var allowUnverified = Environment.GetCommandLineArgs().Contains("--lcvr-skip-shasum");

            if (!VerifyGameVersion())
            {
                if (allowUnverified)
                {
                    Logger.LogError("Warning: Unsupported game version, or corrupted game detected!");
                    Flags |= Flags.InvalidGameAssembly;
                }
                else
                {
                    Logger.LogError("Error: Unsupported game version, or corrupted game detected!");
                    Logger.LogError("Aborting before we blow something up!");

                    return;
                }
            }

            if (!LoadEarlyRuntimeDependencies())
            {
                Logger.LogError("Disabling mod because required runtime dependencies could not be loaded!");
                return;
            }

            if (!AssetManager.LoadAssets())
            {
                Logger.LogError("Disabling mod because assets could not be loaded!");
                return;
            }

            // Enable dynamic resolution (if enabled in config)
            var asset = QualitySettings.renderPipeline as HDRenderPipelineAsset;
            var settings = asset.currentPlatformRenderPipelineSettings;

            settings.dynamicResolutionSettings.enabled = Config.EnableUpscaling.Value;
            settings.dynamicResolutionSettings.enableDLSS = Config.EnableDLSS.Value;
            settings.dynamicResolutionSettings.dynResType = DynamicResolutionType.Hardware;
            settings.dynamicResolutionSettings.upsampleFilter = DynamicResUpscaleFilter.CatmullRom;
            settings.dynamicResolutionSettings.minPercentage = Config.ResolutionPercentage.Value;
            settings.dynamicResolutionSettings.maxPercentage = Config.ResolutionPercentage.Value;

            settings.xrSettings.singlePass = false;

            if (Config.LODBias.Value != -1f)
                settings.lodBias = new FloatScalableSetting([Config.LODBias.Value, Config.LODBias.Value, Config.LODBias.Value], ScalableSettingSchemaId.With3Levels);

            asset.currentPlatformRenderPipelineSettings = settings;

            if (!disableVr && InitVRLoader())
                Flags |= Flags.VR;

            HarmonyPatcher.PatchUniversal();

            Logger.LogDebug("Inserted universal patches using Harmony");
        }

        private bool VerifyGameVersion()
        {
            var location = typeof(PlayerControllerB).Assembly.Location;

            using var hash = SHA256.Create();
            var bytes = hash.ComputeHash(File.ReadAllBytes(location));
            var shasum = BitConverter.ToString(bytes).Replace("-", "");

            return GAME_ASSEMBLY_HASHES.Contains(shasum);
        }

        private bool LoadEarlyRuntimeDependencies()
        {
            try
            {
                var current = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var deps = Path.Combine(current, "RuntimeDeps");

                foreach (var file in Directory.GetFiles(deps, "*.dll"))
                {
                    var filename = Path.GetFileName(file);

                    // Ignore known unmanaged libraries
                    if (filename == "UnityOpenXR.dll" || filename == "openxr_loader.dll")
                        continue;

                    Logger.LogDebug($"Early loading {filename}");

                    try
                    {
                        Assembly.LoadFile(file);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"Failed to early load {filename}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Unexpected error occured while loading early runtime dependencies (incorrect folder structure?): {ex.Message}");
                return false;
            }


            return true;
        }

        private bool InitVRLoader()
        {
            Logger.LogInfo("Loading VR...");

            if (SetupRuntimeAssets())
            {
                Logger.LogError("You must restart the game to allow VR to function properly");
                Flags |= Flags.RestartRequired;

                return false;
            }

            EnableControllerProfiles();
            InitializeXRRuntime();

            if (!StartDisplay())
            {
                OpenXR.DumpOpenXRDiag();

                Logger.LogError("Failed to start in VR Mode! Only Non-VR features are available!");
                Logger.LogError("An OpenXR log dump is displayed above to help with troubleshooting.");

                return false;
            }

            HarmonyPatcher.PatchVR();

            Logger.LogDebug("Inserted VR patches using Harmony");

            return true;
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
            OpenXRSettings.Instance.depthSubmissionMode = OpenXRSettings.DepthSubmissionMode.Depth24Bit;

            typeof(XRGeneralSettings).GetMethod("InitXRSDK", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(generalSettings, []);
            typeof(XRGeneralSettings).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(generalSettings, []);

            Logger.LogInfo("Initialized OpenXR Runtime");
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

            Logger.LogInfo("Started XR Display subsystem, welcome to VR!");

            return true;
        }

        /// <summary>
        /// Place required runtime libraries and configuration in the game files to allow VR to be started
        /// </summary>
        private bool SetupRuntimeAssets()
        {
            var mustRestart = false;

            var root = Path.Combine(Paths.GameRootPath, "Lethal Company_Data");
            var subsystems = Path.Combine(root, "UnitySubsystems");
            if (!Directory.Exists(subsystems))
                Directory.CreateDirectory(subsystems);

            var openXr = Path.Combine(subsystems, "UnityOpenXR");
            if (!Directory.Exists(openXr))
                Directory.CreateDirectory(openXr);

            var manifest = Path.Combine(openXr, "UnitySubsystemsManifest.json");
            if (!File.Exists(manifest))
            {
                File.WriteAllText(manifest, Properties.Resources.UnitySubsystemsManifest);
                mustRestart = true;
            }

            var plugins = Path.Combine(root, "Plugins");
            var uoxrTarget = Path.Combine(plugins, "UnityOpenXR.dll");
            var oxrLoaderTarget = Path.Combine(plugins, "openxr_loader.dll");

            var current = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var uoxr = Path.Combine(current, "RuntimeDeps/UnityOpenXR.dll");
            var oxrLoader = Path.Combine(current, "RuntimeDeps/openxr_loader.dll");

            if (File.Exists(uoxr))
                File.Copy(uoxr, uoxrTarget, true);
            else
                Logger.LogWarning("Could not find UnityOpenXR.dll to copy to the game, VR might not work!");

            if (File.Exists(oxrLoader))
                File.Copy(oxrLoader, oxrLoaderTarget, true);
            else
                Logger.LogWarning("Could not find openxr_loader.dll to copy to the game, VR might not work!");

            return mustRestart;
        }
    }

    [Flags]
    public enum Flags : byte
    {
        VR = 1,
        RestartRequired = 2,
        UnityExplorerDetected = 4,
        InvalidGameAssembly = 8
    }
}
