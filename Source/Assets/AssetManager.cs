using LCVR.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LCVR.Assets;

internal static class AssetManager
{
    private static AssetBundle assetBundle;

    public static GameObject Interactable;
    public static GameObject Keyboard;
    public static GameObject SettingsPanel;
    public static GameObject KeybindDiscard;
    public static GameObject VolumeManager;
    public static GameObject SpectatorLight;
    public static GameObject SpectatorGhost;
    public static GameObject EnemyPrefab;
    public static GameObject SteeringWheelPoints;

    public static Material SplashMaterial;
    public static Material DefaultRayMat;

    public static Shader TMPAlwaysOnTop;
    
    public static InputActionAsset VRActions;
    public static InputActionAsset DefaultXRActions;
    public static InputActionAsset NullActions;

    public static RemappableControls RemappableControls;

    public static Sprite GithubImage;
    public static Sprite KofiImage;
    public static Sprite DiscordImage;
    public static Sprite WarningImage;
    public static Sprite SettingsImage;

    public static AudioClip DoorLocked;

    public static bool LoadAssets()
    {
        assetBundle = AssetBundle.LoadFromMemory(Properties.Resources.lethalcompanyvr);

        if (assetBundle == null)
        {
            Logger.LogError("Failed to load asset bundle!");
            return false;
        }
        
        Interactable = assetBundle.LoadAsset<GameObject>("VRInteractable");
        Keyboard = assetBundle.LoadAsset<GameObject>("NonNativeKeyboard");
        SettingsPanel = assetBundle.LoadAsset<GameObject>("Panel");
        KeybindDiscard = assetBundle.LoadAsset<GameObject>("KeybindDiscard");
        VolumeManager = assetBundle.LoadAsset<GameObject>("Volume Manager");
        EnemyPrefab = assetBundle.LoadAsset<GameObject>("CaveDwellerEnemy");
        SpectatorLight = assetBundle.LoadAsset<GameObject>("Spectator Light");
        SpectatorGhost = assetBundle.LoadAsset<GameObject>("SpectatorGhost");
        SteeringWheelPoints = assetBundle.LoadAsset<GameObject>("SnapPointContainer");

        VRActions = assetBundle.LoadAsset<InputActionAsset>("VRActions");
        DefaultXRActions = assetBundle.LoadAsset<InputActionAsset>("DefaultXRActions");
        NullActions = assetBundle.LoadAsset<InputActionAsset>("NullPlayerActions");

        TMPAlwaysOnTop = assetBundle.LoadAsset<Shader>("TextMeshPro Always On Top");
        
        RemappableControls =
            assetBundle.LoadAsset<GameObject>("Remappable Controls").GetComponent<RemappableControls>();

        SplashMaterial = assetBundle.LoadAsset<Material>("Splash");
        DefaultRayMat = assetBundle.LoadAsset<Material>("Default Ray");

        GithubImage = assetBundle.LoadAsset<Sprite>("Github");
        KofiImage = assetBundle.LoadAsset<Sprite>("Ko-Fi");
        DiscordImage = assetBundle.LoadAsset<Sprite>("Discord");
        WarningImage = assetBundle.LoadAsset<Sprite>("Warning");
        SettingsImage = assetBundle.LoadAsset<Sprite>("lcsettings-icon");

        DoorLocked = assetBundle.LoadAsset<AudioClip>("doorlocked");

        if (RemappableControls == null || RemappableControls.controls == null)
        {
            Logger.LogError(
                "Unity failed to deserialize some assets. Are you missing the FixPluginTypesSerialization mod?");
            return false;
        }

        return true;
    }
}
