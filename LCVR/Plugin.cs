using BepInEx;
using BepInEx.Bootstrap;
using GameNetcodeStuff;
using LCVR.Assets;
using LCVR.Patches;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;

using DependencyFlags = BepInEx.BepInDependency.DependencyFlags;

namespace LCVR
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    #region Compatibility Dependencies
    [BepInDependency("me.swipez.melonloader.morecompany", DependencyFlags.SoftDependency)]
    [BepInDependency("x753.Mimics", DependencyFlags.SoftDependency)]
    [BepInDependency("FlipMods.TooManyEmotes", DependencyFlags.SoftDependency)]
    #endregion
    public class Plugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "io.daxcess.lcvr";
        public const string PLUGIN_NAME = "LCVR";
        public const string PLUGIN_VERSION = "1.1.2";

        private readonly string[] GAME_ASSEMBLY_HASHES = [
            "AAC6149C355A19865C0F67FD0C1D7111D4F418EF94D700265B591665B4CDCE73", // V45
            "3EE687F8586F8597BA9E750E5C75141CA353C0076A3FC3C802AE9CE35D876580"  // V49
        ];

        public static new Config Config { get; private set; }
        public static Compat Compatibility { get; private set; }
        public static Flags Flags { get; private set; } = 0;

        private void Awake()
        {
            // Fix XR not working with non-english PC languages
            // Again, why the fuck do we need another hack to make shit just work normally?
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            // Reload Unity's Input System plugins since BepInEx in some
            // configurations runs after the Input System has already been initialized
            typeof(InputSystem).GetMethod("PerformDefaultPluginInitialization", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, []);

            // Plugin startup logic
            LCVR.Logger.SetSource(Logger);

            Config = new Config(base.Config);
            Compatibility = new Compat([.. Chainloader.PluginInfos.Values]);

            Logger.LogInfo($"Plugin {PLUGIN_NAME} is starting...");

            // Allow disabling VR via config and command line
            var disableVr = Config.DisableVR.Value ||
                            Environment.GetCommandLineArgs().Contains("--disable-vr", StringComparer.OrdinalIgnoreCase);

            if (disableVr)
                Logger.LogWarning("VR has been disabled by config or the `--disable-vr` command line flag");

            // Verify game assembly to detect compatible version
            var allowUnverified = Environment.GetCommandLineArgs().Contains("--lcvr-skip-checksum");

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

            // Change HDRP settings
            var asset = QualitySettings.renderPipeline as HDRenderPipelineAsset;
            var settings = asset.currentPlatformRenderPipelineSettings;

            if (Config.EnableDLSS.Value)
            {
                settings.dynamicResolutionSettings.enabled = true;
                settings.dynamicResolutionSettings.enableDLSS = true;
                settings.supportMotionVectors = true;
            }

            settings.xrSettings.occlusionMesh = false;
            settings.xrSettings.singlePass = false;

            if (Config.LODBias.Value != -1f)
                settings.lodBias = new FloatScalableSetting([Config.LODBias.Value, Config.LODBias.Value, Config.LODBias.Value], ScalableSettingSchemaId.With3Levels);

            asset.currentPlatformRenderPipelineSettings = settings;

            if (!disableVr && InitVRLoader())
                Flags |= Flags.VR;

            HarmonyPatcher.PatchUniversal();

            Logger.LogDebug("Inserted universal patches using Harmony");

            // Bring game window to front
            Native.BringGameWindowToFront();
        }

        private bool VerifyGameVersion()
        {
            var location = typeof(PlayerControllerB).Assembly.Location;
            var shasum = BitConverter.ToString(Utils.ComputeHash(File.ReadAllBytes(location))).Replace("-", "");

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
                Logger.LogError("Failed to start in VR Mode! Only Non-VR features are available!");

                if (OpenXR.GetDiagnosticReport(out var report))
                {
                    Logger.LogWarning($"Runtime Name:    {report.RuntimeName}");
                    Logger.LogWarning($"Runtime Version: {report.RuntimeVersion}");
                    Logger.LogWarning($"Last Error:      {report.Error}");
                    Logger.LogWarning("");

                    switch (report.Error)
                    {
                        case "XR_ERROR_RUNTIME_UNAVAILABLE":
                            Logger.LogWarning("It appears that no OpenXR runtime is currently active. Please go to the dedicated application for your VR headset and make sure that it is running, and set as default OpenXR runtime.");
                            break;

                        case "XR_ERROR_FORM_FACTOR_UNAVAILABLE":
                            Logger.LogWarning("This generally means that your headset is not connected, or that your headset is connected to a different runtime. Please make sure your headset is active and connected, and that you are using the correct OpenXR runtime.");
                            break;

                        default:
                            Logger.LogWarning("Unknown reason for OpenXR failure!");
                            Logger.LogWarning($"\n{OpenXR.GenerateReport()}");
                            break;
                    }
                }
                else Logger.LogError("Failed to generate OpenXR diagnostics report!");

                var runtimes = OpenXR.DetectOpenXRRuntimes();
                if (runtimes != null)
                {
                    Logger.LogWarning("List of registered OpenXR runtimes on this device:");

                    for (var i = 0; i < runtimes.Length; i++)
                        Logger.LogWarning($"{(i == 0 ? ">>> " : "    ")}{runtimes[i]}");
                }

                return false;
            }

            foreach (var plugin in Chainloader.PluginInfos.Values)
                if (plugin.Metadata.GUID == "com.sinai.unityexplorer")
                {
                    Logger.LogWarning("WARNING: UNITY EXPLORER DETECTED! UNITY EXPLORER *WILL* BREAK VR UI INPUTS!");
                    Flags |= Flags.UnityExplorerDetected;
                }


            if (OpenXR.GetRuntimeName(out var name) && OpenXR.GetRuntimeVersion(out var major, out var minor, out var patch))
                Logger.LogInfo($"OpenXR runtime being used: {name} ({major}.{minor}.{patch})");
            else
                Logger.LogError("Could not get runtime OpenXR name?");

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

            if (Config.CameraResolutionGlobal.Value)
                XRSettings.eyeTextureResolutionScale = Config.CameraResolution.Value;

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
        /// Helper function for SetupRuntimeAssets() to copy resource files and return false if the source does not exist
        /// </summary>
        private bool CopyResourceFile(string sourceFile, string destinationFile)
        {
            if (!File.Exists(sourceFile))
                return false;

            if (File.Exists(destinationFile))
            {
                var sourceHash = Utils.ComputeHash(File.ReadAllBytes(sourceFile));
                var destHash = Utils.ComputeHash(File.ReadAllBytes(destinationFile));

                if (sourceHash.SequenceEqual(destHash))
                    return true;
            }

            File.Copy(sourceFile, destinationFile, true);

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

            if (!CopyResourceFile(uoxr, uoxrTarget))
                Logger.LogWarning("Could not find UnityOpenXR.dll to copy to the game, VR might not work!");

            if (!CopyResourceFile(oxrLoader, oxrLoaderTarget))
                Logger.LogWarning("Could not find openxr_loader.dll to copy to the game, VR might not work!");

            return mustRestart;
        }
    }

    [Flags]
    public enum Flags
    {
        VR = 1,
        RestartRequired = 2,
        UnityExplorerDetected = 4,
        InvalidGameAssembly = 8
    }
}
