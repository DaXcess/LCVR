using BepInEx;
using BepInEx.Bootstrap;
using LCVR.Assets;
using LCVR.Patches;
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.UI;

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
    public const string PLUGIN_VERSION = "1.2.4";

    private readonly string[] GAME_ASSEMBLY_HASHES =
    [
        "7CFABBA203022CC46EF309B0E651276CB59217AF6D38C34E2085E67957DBBCBD", // V50
        "4C265CECBC1A075E52D9E1FA458C67AA25C087362B472DF66DF370B9A0676A67", // V50 Patch 1
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

        // Force the XR Interaction Toolkit assembly to load before we load asset bundles
        _ = TrackedDeviceGraphicRaycaster.s_Corners;

        // Plugin startup logic
        LCVR.Logger.SetSource(Logger);

        Config = new Config(base.Config);
        Compatibility = new Compat([.. Chainloader.PluginInfos.Values]);

        Logger.LogInfo($"Plugin {PLUGIN_NAME} is starting...");

        // Extract LCVR dependencies
        if (!ExtractPackage(out var mustRestart))
        {
            Logger.LogError("Failed to extract LCVR dependencies, disabling mod");
            return;
        }
        
        if (mustRestart)
            Flags |= Flags.RestartRequired;

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

    private bool VerifyGameVersion()
    {
        var location = Path.Combine(Paths.ManagedPath, "Assembly-CSharp.dll");
        var shasum = BitConverter.ToString(Utils.ComputeHash(File.ReadAllBytes(location))).Replace("-", "");

        return GAME_ASSEMBLY_HASHES.Contains(shasum);
    }

    /// <summary>
    /// Verifies and extracts the LCVR dependencies (if necessary), returning whether the game needs to restart
    /// </summary>
    private bool ExtractPackage(out bool mustRestart)
    {
        mustRestart = false;

        try
        {
            var basePath = Path.Combine(Paths.GameRootPath, "Lethal Company_Data");
            using var zip = ZipFile.OpenRead(Path.Combine(Info.Location, "package"));

            foreach (var entry in zip.Entries.Where(entry =>
                         !entry.FullName.EndsWith('/') || !string.IsNullOrEmpty(entry.Name)))
            {
                var fullPath = Path.Combine(basePath, entry.FullName);
                var directoryName = Path.GetDirectoryName(fullPath)!;

                if (!Directory.Exists(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                    mustRestart = true;
                }

                using var stream = entry.Open();
                using var reader = new BinaryReader(stream);

                var bytes = reader.ReadBytes((int)entry.Length);

                // Check if file is up-to-date
                if (File.Exists(fullPath) &&
                    Utils.ComputeHash(bytes).SequenceEqual(Utils.ComputeHash(File.ReadAllBytes(fullPath)))) continue;

                File.WriteAllBytes(fullPath, bytes);

                mustRestart = true;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to validate and extract LCVR package: {ex.Message}");
            return false;
        }

        return true;
    }

    private bool InitializeVR()
    {
        Logger.LogInfo("Loading VR...");

        if (!OpenXR.Loader.InitializeXR())
        {
            Logger.LogError("Failed to start in VR Mode! Only Non-VR features are available!");

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
        var settings = asset.currentPlatformRenderPipelineSettings;

        settings.dynamicResolutionSettings.enabled = Config.EnableDynamicResolution.Value;
        settings.dynamicResolutionSettings.enableDLSS = Config.EnableDLSS.Value;
        settings.dynamicResolutionSettings.dynResType = DynamicResolutionType.Hardware;
        settings.dynamicResolutionSettings.upsampleFilter = Config.DynamicResolutionUpscaleFilter.Value;
        settings.dynamicResolutionSettings.minPercentage = settings.dynamicResolutionSettings.maxPercentage =
            Config.DynamicResolutionPercentage.Value;
        settings.supportMotionVectors = true;

        if (Config.EnableDLSS.Value)
            Logger.LogWarning(
                "DLSS has been deprecated, and will be removed in a future release. Please switch over to the Dynamic Resolution and Camera Resolution configuration to enhance your performance.");

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

        mesh.material = AssetManager.splashMaterial;
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
