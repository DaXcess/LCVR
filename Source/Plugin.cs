using BepInEx;
using BepInEx.Bootstrap;
using LCVR.Assets;
using LCVR.Patches;
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

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
    public const string PLUGIN_VERSION = "1.3.0";

    private readonly string[] GAME_ASSEMBLY_HASHES =
    [
        "A6B2633FE729B9C147466CD4A92168872EF789620EB29FF723A33937837AC9B0", // V56
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

        Logger.LogInfo($"Starting {PLUGIN_NAME} v{PLUGIN_VERSION} ({GetCommitHash()})");

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
                Logger.LogError("This usually happens if Lethal Company got updated recently.");
                Logger.LogWarning(
                    "To bypass this check, add the following flag to your launch options in Steam: --lcvr-skip-checksum");
                
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

        if (!disableVr && InitializeVR())
        {
            Flags |= Flags.VR;
            Flags &= ~Flags.RestartRequired;

            StartCoroutine(HijackSplashScreen());
        }

        HarmonyPatcher.PatchUniversal();

        Logger.LogDebug("Inserted universal patches using Harmony");

        // Bring game window to front
        Native.BringGameWindowToFront();
    }

    private static string GetCommitHash()
    {
        var attribute = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>();

        return attribute?.InformationalVersion.Split('+')[1][..7] ?? "unknown";
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
            var deps = Path.Combine(Path.GetDirectoryName(Info.Location)!, "RuntimeDeps");

            foreach (var file in Directory.GetFiles(deps, "*.dll"))
            {
                var filename = Path.GetFileName(file);

                // Ignore known unmanaged libraries
                if (filename is "UnityOpenXR.dll" or "openxr_loader.dll")
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

    private bool InitializeVR()
    {
        Logger.LogInfo("Loading VR...");
        
        if (SetupRuntimeAssets())
        {
            Logger.LogWarning("You might have to restart the game to allow VR to function properly");
            Flags |= Flags.RestartRequired;
        }

        if (!OpenXR.Loader.InitializeXR())
        {
            Logger.LogError("Failed to start in VR Mode! Only Non-VR features are available!");
            Logger.LogWarning("If you are not intending to play in VR, you can ignore the previous error.");

            return false;
        }

        if (OpenXR.GetActiveRuntimeName(out var name) &&
            OpenXR.GetActiveRuntimeVersion(out var major, out var minor, out var patch))
            Logger.LogInfo($"OpenXR runtime being used: {name} ({major}.{minor}.{patch})");
        else
            Logger.LogError("Could not get OpenXR runtime info?");

        HarmonyPatcher.PatchVR();

        Logger.LogDebug("Inserted VR patches using Harmony");

        // Change HDRP settings
        var asset = QualitySettings.renderPipeline as HDRenderPipelineAsset;
        var settings = asset!.currentPlatformRenderPipelineSettings;

        settings.dynamicResolutionSettings.enabled = Config.EnableDynamicResolution.Value;
        settings.dynamicResolutionSettings.dynResType = DynamicResolutionType.Hardware;
        settings.dynamicResolutionSettings.upsampleFilter = Config.DynamicResolutionUpscaleFilter.Value;
        settings.dynamicResolutionSettings.minPercentage = settings.dynamicResolutionSettings.maxPercentage =
            Config.DynamicResolutionPercentage.Value;
        settings.supportMotionVectors = true;

        settings.xrSettings.occlusionMesh = false;
        settings.xrSettings.singlePass = false;

        settings.lodBias = new FloatScalableSetting([Config.LODBias.Value, Config.LODBias.Value, Config.LODBias.Value],
            ScalableSettingSchemaId.With3Levels);

        asset.currentPlatformRenderPipelineSettings = settings;

        return true;
    }

    /// <summary>
    /// Modify the splash screen logo
    /// </summary>
    private static IEnumerator HijackSplashScreen()
    {
        yield return new WaitUntil(() =>
        {
            try
            {
                SceneManager.GetActiveScene().GetRootGameObjects();
                return true;
            }
            catch
            {
                return false;
            }
        });

        var mesh = GameObject.Find("SplashRootObject/Quad").GetComponent<MeshRenderer>();

        mesh.material = AssetManager.SplashMaterial;
    }
}

[Flags]
public enum Flags
{
    VR = 1 << 0,
    RestartRequired = 1 << 1,
    InvalidGameAssembly = 1 << 2,
    InteractableDebug = 1 << 3,
}
