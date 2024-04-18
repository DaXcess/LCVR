using BepInEx;
using BepInEx.Bootstrap;
using GameNetcodeStuff;
using LCVR.Assets;
using LCVR.Patches;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;

using DependencyFlags = BepInEx.BepInDependency.DependencyFlags;

namespace LCVR;

[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
#region Compatibility Dependencies
[BepInDependency("me.swipez.melonloader.morecompany", DependencyFlags.SoftDependency)]
[BepInDependency("x753.Mimics", DependencyFlags.SoftDependency)]
[BepInDependency("com.fumiko.CullFactory", DependencyFlags.SoftDependency)]
#endregion
public class Plugin : BaseUnityPlugin
{
    public const string PLUGIN_GUID = "io.daxcess.lcvr";
    public const string PLUGIN_NAME = "LCVR";
    public const string PLUGIN_VERSION = "1.2.2";

    private readonly string[] GAME_ASSEMBLY_HASHES = [
        "7CFABBA203022CC46EF309B0E651276CB59217AF6D38C34E2085E67957DBBCBD",  // V50
        "4C265CECBC1A075E52D9E1FA458C67AA25C087362B472DF66DF370B9A0676A67",  // V50 Patch 1
    ];

    public new static Config Config { get; private set; }
    public static Compat Compatibility { get; private set; }
    public static Flags Flags { get; private set; } = 0;

    private void Awake()
    {
        // Fix XR not working with non-english PC languages
        // Why isn't this the default in LC??
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        // Reload Unity's Input System plugins since BepInEx in some
        // configurations runs after the Input System has already been initialized
        InputSystem.PerformDefaultPluginInitialization();

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
        else if (Config.AskOnStartup.Value)
        {
            var response = Native.ShellMessageBox(IntPtr.Zero, IntPtr.Zero,
                "Do you want to run Lethal Company in VR mode?\n\nDon't forget to connect your headset first before launching the game in VR.\nLaunching the game without VR will still allow you to see other VR players' arm movements.\n\nPress 'Cancel' to close the game.",
                "Lethal Company VR Mod", 0x1043);

            switch (response)
            {
                case 2:
                    // Application.Quit crashes the game, presumably since this code runs in the constructor of Application which messes things up
                    System.Diagnostics.Process.GetCurrentProcess().Kill();
                    return;
                
                case 7:
                    disableVr = true;
                    break;
            } 
        }

        if (Environment.GetCommandLineArgs().Contains("--lcvr-debug-interactables"))
            Flags |= Flags.InteractableDebug;

        // Verify game assembly to detect compatible version
        var allowUnverified = Environment.GetCommandLineArgs().Contains("--lcvr-skip-checksum");

        if (!VerifyGameVersion())
        {
            if (allowUnverified)
            {
                Logger.LogWarning("Warning: Unsupported game version, or corrupted game detected!");
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

        if (!disableVr && InitVRLoader())
        {
            Flags |= Flags.VR;

            StartCoroutine(HijackSplashScreen());
        }

        HarmonyPatcher.PatchUniversal();

        Logger.LogDebug("Inserted universal patches using Harmony");

        // Bring game window to front
        Native.BringGameWindowToFront();
    }

    private bool VerifyGameVersion()
    {
        var location = Path.Combine(Paths.ManagedPath, "Assembly-CSharp.dll");
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

        if (!string.IsNullOrEmpty(Config.OpenXRRuntimeFile.Value))
            Environment.SetEnvironmentVariable("XR_RUNTIME_JSON", Config.OpenXRRuntimeFile.Value);

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

            var runtimes = OpenXR.DetectOpenXRRuntimes(out var defaultRuntime);
            if (runtimes != null)
            {
                Logger.LogWarning("List of registered OpenXR runtimes on this device:");

                if (defaultRuntime != null)
                    Logger.LogWarning($">>> {defaultRuntime}");
                else
                    Logger.LogWarning("No default runtime detected!");

                foreach (var rt in runtimes.Keys)
                {
                    if (rt == defaultRuntime) continue;

                    Logger.LogWarning($"    {rt}");
                }
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

        // Change HDRP settings
        var asset = QualitySettings.renderPipeline as HDRenderPipelineAsset;
        var settings = asset.currentPlatformRenderPipelineSettings;

        settings.dynamicResolutionSettings.enabled = Config.EnableDynamicResolution.Value;
        settings.dynamicResolutionSettings.enableDLSS = Config.EnableDLSS.Value;
        settings.dynamicResolutionSettings.dynResType = DynamicResolutionType.Hardware;
        settings.dynamicResolutionSettings.upsampleFilter = Config.DynamicResolutionUpscaleFilter.Value;
        settings.dynamicResolutionSettings.minPercentage = settings.dynamicResolutionSettings.maxPercentage = Config.DynamicResolutionPercentage.Value;
        settings.supportMotionVectors = true;

        settings.xrSettings.occlusionMesh = false;
        settings.xrSettings.singlePass = false;
        
        settings.lodBias = new FloatScalableSetting([Config.LODBias.Value, Config.LODBias.Value, Config.LODBias.Value], ScalableSettingSchemaId.With3Levels);

        asset.currentPlatformRenderPipelineSettings = settings;

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

        // This feature list is empty by default if the game isn't a VR game
        OpenXRSettings.Instance.features = [
            valveIndex,
            hpReverb,
            htcVive,
            mmController,
            khrSimple,
            metaQuestTouch,
            oculusTouch
        ];

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
        OpenXRSettings.Instance.depthSubmissionMode = OpenXRSettings.DepthSubmissionMode.None;

        // Initialize XR
        generalSettings.InitXRSDK();
        generalSettings.Start();

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

    /// <summary>
    /// Modify the splash screen logo
    /// </summary>
    private IEnumerator HijackSplashScreen()
    {
        yield return new WaitUntil(() =>
        {
            try
            {
                SceneManager.GetActiveScene().GetRootGameObjects();
                return true;
            }
            catch { return false; }
        });

        var mesh = GameObject.Find("SplashRootObject/Quad").GetComponent<MeshRenderer>();

        mesh.material = AssetManager.splashMaterial;
    }
}

[Flags]
public enum Flags
{
    VR = 1 << 0,
    RestartRequired = 1 << 1,
    UnityExplorerDetected = 1 << 2,
    InvalidGameAssembly = 1 << 3,
    InteractableDebug = 1 << 4,
}
