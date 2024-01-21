using UnityEngine;
using UnityEngine.InputSystem;

namespace LCVR.Assets
{
    public class AssetManager
    {
        private static AssetBundle assetBundle;

        public static GameObject aLiteralCube;
        public static GameObject cockroach;
        public static GameObject keyboard;
        public static GameObject leftHand;
        public static GameObject rightHand;

        public static Material defaultRayMat;
        public static Material alwaysOnTopMat;

        public static InputActionAsset defaultInputActions;

        public static RuntimeAnimatorController localVrMetarig;
        public static RuntimeAnimatorController remoteVrMetarig;

        public static Sprite githubImage;
        public static Sprite kofiImage;
        public static Sprite discordImage;
        public static Sprite warningImage;

        public static bool LoadAssets()
        {
            assetBundle = AssetBundle.LoadFromMemory(Properties.Resources.lethalcompanyvr);

            if (assetBundle == null)
            {
                Logger.LogError("Failed to load asset bundle!");
                return false;
            }

            aLiteralCube = assetBundle.LoadAsset<GameObject>("ALiteralCube");
            cockroach = assetBundle.LoadAsset<GameObject>("Cockroach");
            keyboard = assetBundle.LoadAsset<GameObject>("NonNativeKeyboard");
            defaultInputActions = assetBundle.LoadAsset<InputActionAsset>("XR Input Actions");
            defaultRayMat = assetBundle.LoadAsset<Material>("Default Ray");
            alwaysOnTopMat = assetBundle.LoadAsset<Material>("Always On Top");
            githubImage = assetBundle.LoadAsset<Sprite>("Github");
            kofiImage = assetBundle.LoadAsset<Sprite>("Ko-Fi");
            discordImage = assetBundle.LoadAsset<Sprite>("Discord");
            warningImage = assetBundle.LoadAsset<Sprite>("Warning");
            localVrMetarig = assetBundle.LoadAsset<RuntimeAnimatorController>("metarig");
            remoteVrMetarig = assetBundle.LoadAsset<RuntimeAnimatorController>("metarigOtherPlayers");

            return true;
        }

        public static InputActionAsset Input(string name)
        {
            return assetBundle.LoadAsset<InputActionAsset>(name);
        }
    }
}
