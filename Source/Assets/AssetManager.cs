using System.IO;
using LCVR.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LCVR.Assets;

public static class AssetManager
{
    private static AssetBundle assetsBundle;
    private static AssetBundle scenesBundle;

    public static GameObject Interactable;
    public static GameObject Keyboard;
    public static GameObject VolumeManager;
    public static GameObject SpectatorLight;
    public static GameObject SpectatorGhost;
    public static GameObject SteeringWheelPoints;
    public static GameObject PopupText;
    public static GameObject SpectatingMenu;
    public static GameObject Reticle;
    
    public static GameObject InitMenuEnvironment;
    public static GameObject MainMenuEnvironment;
    public static GameObject PauseMenuEnvironment;

    public static GameObject SettingsPanel;
    
    public static Material SplashMaterial;
    public static Material DefaultRayMat;

    public static Shader TMPAlwaysOnTop;
    public static Shader VignettePostProcess;
    
    public static InputActionAsset VRActions;
    public static InputActionAsset DefaultXRActions;
    public static InputActionAsset NullActions;

    public static RemappableControls RemappableControls;

    public static Sprite GithubImage;
    public static Sprite KofiImage;
    public static Sprite DiscordImage;
    public static Sprite WarningImage;
    public static Sprite SprintImage;

    public static AudioClip DoorLocked;

    internal static bool LoadAssets()
    {
        assetsBundle =
            AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Plugin.Config.AssemblyPath)!,
                "lethalcompanyvr"));
        scenesBundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Plugin.Config.AssemblyPath)!,
            "lethalcompanyvr-levels"));

        if (assetsBundle == null || scenesBundle == null)
        {
            Logger.LogError("Failed to load asset bundle!");
            return false;
        }
        
        Interactable = assetsBundle.LoadAsset<GameObject>("VRInteractable");
        Keyboard = assetsBundle.LoadAsset<GameObject>("NonNativeKeyboard");
        VolumeManager = assetsBundle.LoadAsset<GameObject>("Volume Manager");
        SpectatorLight = assetsBundle.LoadAsset<GameObject>("Spectator Light");
        SpectatorGhost = assetsBundle.LoadAsset<GameObject>("Spectator Ghost");
        SteeringWheelPoints = assetsBundle.LoadAsset<GameObject>("SnapPointContainer");
        PopupText = assetsBundle.LoadAsset<GameObject>("Popup Text");
        SpectatingMenu = assetsBundle.LoadAsset<GameObject>("Spectating Menu");
        Reticle = assetsBundle.LoadAsset<GameObject>("Reticle");
        
        InitMenuEnvironment = assetsBundle.LoadAsset<GameObject>("Init Menu Environment");
        MainMenuEnvironment = assetsBundle.LoadAsset<GameObject>("Main Menu Environment");
        PauseMenuEnvironment = assetsBundle.LoadAsset<GameObject>("Pause Menu Environment");

        SettingsPanel = assetsBundle.LoadAsset<GameObject>("SettingsPanel");
        
        VRActions = assetsBundle.LoadAsset<InputActionAsset>("VRActions");
        DefaultXRActions = assetsBundle.LoadAsset<InputActionAsset>("DefaultXRActions");
        NullActions = assetsBundle.LoadAsset<InputActionAsset>("NullPlayerActions");

        TMPAlwaysOnTop = assetsBundle.LoadAsset<Shader>("TextMeshPro Always On Top");
        VignettePostProcess = assetsBundle.LoadAsset<Shader>("Vignette");
        
        RemappableControls =
            assetsBundle.LoadAsset<GameObject>("Remappable Controls").GetComponent<RemappableControls>();

        SplashMaterial = assetsBundle.LoadAsset<Material>("Splash");
        DefaultRayMat = assetsBundle.LoadAsset<Material>("Default Ray");

        GithubImage = assetsBundle.LoadAsset<Sprite>("Github");
        KofiImage = assetsBundle.LoadAsset<Sprite>("Ko-Fi");
        DiscordImage = assetsBundle.LoadAsset<Sprite>("Discord");
        WarningImage = assetsBundle.LoadAsset<Sprite>("Warning");
        SprintImage = assetsBundle.LoadAsset<Sprite>("Aguy");

        DoorLocked = assetsBundle.LoadAsset<AudioClip>("doorlocked");

        if (RemappableControls == null || RemappableControls.controls == null)
        {
            Logger.LogError(
                "Unity failed to deserialize some assets. Are you missing the FixPluginTypesSerialization mod?");
            return false;
        }

        return true;
    }
}
