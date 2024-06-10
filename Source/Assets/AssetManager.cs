using LCVR.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LCVR.Assets;

internal static class AssetManager
{
    private static AssetBundle assetBundle;

    public static GameObject aLiteralCube;
    public static GameObject interactable;
    public static GameObject keyboard;
    public static GameObject settingsPanel;
    public static GameObject volumeManager;
    public static GameObject spectatorLight;
    public static GameObject spectatorGhost;
    public static GameObject enemyPrefab;

    public static Material splashMaterial;
    public static Material defaultRayMat;
    public static Material alwaysOnTopMat;

    public static InputActionAsset VRActions;
    public static InputActionAsset TrackingActions;
    public static InputActionAsset NullActions;

    public static RemappableControls RemappableControls;

    public static RuntimeAnimatorController localVrMetarig;
    public static RuntimeAnimatorController remoteVrMetarig;

    public static Sprite githubImage;
    public static Sprite kofiImage;
    public static Sprite discordImage;
    public static Sprite warningImage;
    public static Sprite settingsImage;

    public static AudioClip doorLocked;

    public static bool LoadAssets()
    {
        assetBundle = AssetBundle.LoadFromMemory(Properties.Resources.lethalcompanyvr);
        
        if (assetBundle == null)
        {
            Logger.LogError("Failed to load asset bundle!");
            return false;
        }

        aLiteralCube = assetBundle.LoadAsset<GameObject>("ALiteralCube");
        interactable = assetBundle.LoadAsset<GameObject>("VRInteractable");
        keyboard = assetBundle.LoadAsset<GameObject>("NonNativeKeyboard");
        settingsPanel = assetBundle.LoadAsset<GameObject>("Panel");
        volumeManager = assetBundle.LoadAsset<GameObject>("Volume Manager");
        enemyPrefab = assetBundle.LoadAsset<GameObject>("Flowerman");
        spectatorLight = assetBundle.LoadAsset<GameObject>("Spectator Light");
        spectatorGhost = assetBundle.LoadAsset<GameObject>("SpectatorGhost");
        
        VRActions = assetBundle.LoadAsset<InputActionAsset>("VRActions");
        TrackingActions = assetBundle.LoadAsset<InputActionAsset>("TrackingActions");
        NullActions = assetBundle.LoadAsset<InputActionAsset>("NullPlayerActions");

        RemappableControls = assetBundle.LoadAsset<GameObject>("Remappable Controls").GetComponent<RemappableControls>();
        
        splashMaterial = assetBundle.LoadAsset<Material>("Splash");
        defaultRayMat = assetBundle.LoadAsset<Material>("Default Ray");
        alwaysOnTopMat = assetBundle.LoadAsset<Material>("Always On Top");

        githubImage = assetBundle.LoadAsset<Sprite>("Github");
        kofiImage = assetBundle.LoadAsset<Sprite>("Ko-Fi");
        discordImage = assetBundle.LoadAsset<Sprite>("Discord");
        warningImage = assetBundle.LoadAsset<Sprite>("Warning");
        settingsImage = assetBundle.LoadAsset<Sprite>("lcsettings-icon-2");

        localVrMetarig = assetBundle.LoadAsset<RuntimeAnimatorController>("metarig");
        remoteVrMetarig = assetBundle.LoadAsset<RuntimeAnimatorController>("metarigOtherPlayers");

        doorLocked = assetBundle.LoadAsset<AudioClip>("doorlocked");

        return true;
    }

    public static InputActionAsset Input(string name)
    {
        return assetBundle.LoadAsset<InputActionAsset>(name);
    }
}
