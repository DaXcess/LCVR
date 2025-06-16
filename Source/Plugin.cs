using BepInEx;
using LCVR.Assets;
using LCVR.Patches;
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Composites;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Interactions;
using DependencyFlags = BepInEx.BepInDependency.DependencyFlags;
using VolumeManager = LCVR.Rendering.VolumeManager;

namespace LCVR;

[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
[BepInDependency(Compat.MoreCompany, DependencyFlags.SoftDependency)]
[BepInDependency(Compat.CullFactory, DependencyFlags.SoftDependency)]
public class Plugin : BaseUnityPlugin
{
    public const string PLUGIN_GUID = "io.daxcess.lcvr";
    public const string PLUGIN_NAME = "LCVR";
    public const string PLUGIN_VERSION = "1.4.0";

#if DEBUG
    private const string SKIP_CHECKSUM_VAR = $"--lcvr-skip-checksum={PLUGIN_VERSION}-dev";
#else
    private const string SKIP_CHECKSUM_VAR = $"--lcvr-skip-checksum={PLUGIN_VERSION}";
#endif
    
    private const string HASHES_OVERRIDE_URL = "https://gist.githubusercontent.com/DaXcess/72c4fbac0f18c76ebc99e6b769f19389/raw/LCVR%2520Game%2520Hashes";

    private readonly string[] GAME_ASSEMBLY_HASHES =
    [
        "486013AE3C5092F424A36690D4E5590D0ABD392C602D3E659788B47C64B5C2FA", // V72
    ];

    public new static Config Config { get; private set; }
    public static Flags Flags { get; private set; } = 0;

    private void Awake()
    {
        // Fix XR not working with non-english PC languages
        // Why isn't this the default in LC??
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        // Reload Unity's Input System plugins since BepInEx in some
        // configurations runs after the Input System has already been initialized
        InputSystem.PerformDefaultPluginInitialization();
        
        // Register XR Toolkit (these normally load during RuntimeInitializeOnLoad -> BeforeSceneLoad)
        ButtonFallbackComposite.Initialize();
        IntegerFallbackComposite.Initialize();
        QuaternionFallbackComposite.Initialize();
        Vector3FallbackComposite.Initialize();
        SectorInteraction.Initialize();
        
        // Plugin startup logic
        LCVR.Logger.SetSource(Logger);
        
        Config = new Config(Info.Location, base.Config);
        Config.DeserializeFromES3();
        Config.File.SettingChanged += (_, _) => Config.SerializeToES3(); 

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

        var args = Environment.GetCommandLineArgs();

        if (args.Contains("--lcvr-debug-interactables"))
            Flags |= Flags.InteractableDebug;

        if (args.Contains("--lcvr-item-offset-editor"))
            Flags |= Flags.ItemOffsetEditor;

        // Verify game assembly to detect compatible version
        var allowUnverified = Environment.GetCommandLineArgs().Contains(SKIP_CHECKSUM_VAR);

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
                Logger.LogDebug(
                    $"To bypass this check, add the following flag to your launch options in Steam: {SKIP_CHECKSUM_VAR}");

                return;
            }
        }

        // Load required dependencies
        if (!PreloadRuntimeDependencies())
        {
            Logger.LogError("Disabling mod because required runtime dependencies could not be loaded!");
            return;
        }

        // Load VR asset bundle
        if (!AssetManager.LoadAssets())
        {
            Logger.LogError("Disabling mod because assets could not be loaded!");
            return;
        }
        
        // Load custom item configuration
        Player.Items.LoadConfig();

        if (!disableVr && InitializeVR())
        {
            Flags |= Flags.VR;

            StartCoroutine(HijackSplashScreen());
        }

        HarmonyPatcher.PatchUniversal();

        Logger.LogDebug("Inserted universal patches using Harmony");

        // Bring game window to front
        Native.BringGameWindowToFront();
    }

    private static string GetCommitHash()
    {
        try
        {
            var attribute = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>();

            return attribute?.InformationalVersion.Split('+')[1][..7] ?? "unknown";
        }
        catch
        {
            LCVR.Logger.LogWarning(
                "Failed to retrieve commit hash (compiled outside of git repo?).");

            return "unknown";
        }
    }

    private bool VerifyGameVersion()
    {
        var location = Path.Combine(Paths.ManagedPath, "Assembly-CSharp.dll");
        var hash = BitConverter.ToString(Utils.ComputeHash(File.ReadAllBytes(location))).Replace("-", "");

        // Attempt local lookup first
        if (GAME_ASSEMBLY_HASHES.Contains(hash))
        {
            Logger.LogInfo("Game version verified using local hashes");
            
            return true;
        }

        Logger.LogWarning("Failed to verify game version using local hashes, checking remotely for updated hashes...");
        
        // Attempt to fetch a gist with known working assembly hashes
        // This allows me to keep LCVR up and running if the game updates, without code changes
        try
        {
            var contents = new WebClient().DownloadString(HASHES_OVERRIDE_URL);
            var hashes = Utils.ParseConfig(contents);
            
            if (!hashes.Contains(hash))
                return false;

            Logger.LogInfo("Game version verified using remote hashes");

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"Failed to verify using remote hashes: {ex.Message}");
            
            return false;
        }
    }

    private bool PreloadRuntimeDependencies()
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

                Logger.LogDebug($"Preloading '{filename}'...");

                try
                {
                    Assembly.LoadFile(file);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Failed to preload '{filename}': {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(
                $"Unexpected error occured while preloading runtime dependencies (incorrect folder structure?): {ex.Message}");
            return false;
        }

        return true;
    }

    public static void ToggleVR()
    {
        if (Flags.HasFlag(Flags.VR))
        {
            OpenXR.Loader.DeinitializeXR();
            HarmonyPatcher.UnpatchVR();

            Flags &= ~Flags.VR;
        }
        else
        {
            if (!InitializeVR())
                return;

            Flags |= Flags.VR;
            Flags &= ~Flags.StartupFailed;
        }
    }

    private static bool InitializeVR()
    {
        LCVR.Logger.LogInfo("Loading VR...");

        if (!OpenXR.Loader.InitializeXR())
        {
            LCVR.Logger.LogError("Failed to start in VR Mode! Only Non-VR features are available!");
            LCVR.Logger.LogWarning("You may ignore the previous error if you are intending to play without VR");

            Flags |= Flags.StartupFailed;

            return false;
        }

        if (OpenXR.GetActiveRuntimeName(out var name) &&
            OpenXR.GetActiveRuntimeVersion(out var major, out var minor, out var patch))
            LCVR.Logger.LogInfo($"OpenXR runtime being used: {name} ({major}.{minor}.{patch})");
        else
            LCVR.Logger.LogError("Could not get OpenXR runtime info?");

        HarmonyPatcher.PatchVR();

        LCVR.Logger.LogDebug("Inserted VR patches using Harmony");

        // Change HDRP settings
        var asset = QualitySettings.renderPipeline as HDRenderPipelineAsset;
        var settings = asset!.currentPlatformRenderPipelineSettings;

        settings.dynamicResolutionSettings.enabled = Config.EnableDynamicResolution.Value;
        settings.dynamicResolutionSettings.dynResType = DynamicResolutionType.Hardware;
        settings.dynamicResolutionSettings.upsampleFilter = Config.DynamicResolutionUpscaleFilter.Value;
        settings.dynamicResolutionSettings.minPercentage = settings.dynamicResolutionSettings.maxPercentage =
            Config.DynamicResolutionPercentage.Value;
        settings.supportMotionVectors = true;

        settings.xrSettings.occlusionMesh = Config.EnableOcclusionMesh.Value;
        settings.xrSettings.singlePass = false;

        settings.lodBias = new FloatScalableSetting([Config.LODBias.Value, Config.LODBias.Value, Config.LODBias.Value],
            ScalableSettingSchemaId.With3Levels);

        asset.currentPlatformRenderPipelineSettings = settings;

        VolumeManager.RegisterCustomPostProcessShaders();

        // Input settings
        InputSystem.settings.defaultButtonPressPoint = Config.ButtonPressPoint.Value;

        return true;
    }

    // ReSharper disable Unity.PerformanceAnalysis
    
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
    InvalidGameAssembly = 1 << 1,
    InteractableDebug = 1 << 2,
    ItemOffsetEditor = 1 << 3,
    StartupFailed = 1 << 4,
}
