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
    public static GameObject VolumeManager;
    public static GameObject SpectatorLight;
    public static GameObject SpectatorGhost;
    public static GameObject EnemyPrefab;
    public static GameObject SteeringWheelPoints;

    public static Material SplashMaterial;
    public static Material DefaultRayMat;
    public static Material AlwaysOnTopMat;

    public static Shader TMPAlwaysOnTop;

    public static InputActionAsset VRActions;
    public static InputActionAsset TrackingActions;
    public static InputActionAsset NullActions;

    public static RemappableControls RemappableControls;

    public static RuntimeAnimatorController LocalVrMetarig;
    public static RuntimeAnimatorController RemoteVrMetarig;

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
        VolumeManager = assetBundle.LoadAsset<GameObject>("Volume Manager");
        EnemyPrefab = assetBundle.LoadAsset<GameObject>("CompanyCruiser");
        SpectatorLight = assetBundle.LoadAsset<GameObject>("Spectator Light");
        SpectatorGhost = assetBundle.LoadAsset<GameObject>("SpectatorGhost");
        SteeringWheelPoints = assetBundle.LoadAsset<GameObject>("SnapPointContainer");

        VRActions = assetBundle.LoadAsset<InputActionAsset>("VRActions");
        TrackingActions = assetBundle.LoadAsset<InputActionAsset>("TrackingActions");
        NullActions = assetBundle.LoadAsset<InputActionAsset>("NullPlayerActions");

        TMPAlwaysOnTop = assetBundle.LoadAsset<Shader>("TextMeshPro Always On Top");

        RemappableControls =
            assetBundle.LoadAsset<GameObject>("Remappable Controls").GetComponent<RemappableControls>();

        SplashMaterial = assetBundle.LoadAsset<Material>("Splash");
        DefaultRayMat = assetBundle.LoadAsset<Material>("Default Ray");
        AlwaysOnTopMat = assetBundle.LoadAsset<Material>("Always On Top");

        GithubImage = assetBundle.LoadAsset<Sprite>("Github");
        KofiImage = assetBundle.LoadAsset<Sprite>("Ko-Fi");
        DiscordImage = assetBundle.LoadAsset<Sprite>("Discord");
        WarningImage = assetBundle.LoadAsset<Sprite>("Warning");
        SettingsImage = assetBundle.LoadAsset<Sprite>("lcsettings-icon");

        LocalVrMetarig = assetBundle.LoadAsset<RuntimeAnimatorController>("metarig");
        RemoteVrMetarig = assetBundle.LoadAsset<RuntimeAnimatorController>("metarigOtherPlayers");

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
