using UnityEngine;

namespace LethalCompanyVR
{
    public class AssetManager
    {
        private static AssetBundle assetBundle;

        public static GameObject aLiteralCube;
        public static GameObject cockroach;
        public static GameObject leftHand;
        public static GameObject rightHand;

        public static bool LoadAssets()
        {
            assetBundle = AssetBundle.LoadFromMemory(Properties.Resources.lethalcompanyvr);
            var handsBundle = AssetBundle.LoadFromMemory(Properties.Resources.hands);

            if (assetBundle == null)
            {
                Logger.LogError("Failed to load asset bundle!");
                return false;
            }

            if (handsBundle == null)
            {
                Logger.LogError("Failed to load hands asset bundle!");
                return false;
            }

            aLiteralCube = assetBundle.LoadAsset<GameObject>("ALiteralCube");
            cockroach = assetBundle.LoadAsset<GameObject>("Cockroach");
            leftHand = handsBundle.LoadAsset<GameObject>("Left Hand Model");
            rightHand = handsBundle.LoadAsset<GameObject>("Right Hand Model");

            return true;
        }
    }
}
