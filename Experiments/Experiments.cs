using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace LCVR.Experiments
{
    internal class Experiments
    {
        public static void RunExperiments()
        {
            // SpawnShotgun();
            ShowMeTheMoney(10000);
            // SpawnBuyableItem<JetpackItem>("Jetpack");
            // SpawnBuyableItem<SprayPaintItem>("Spray paint");
            // SpawnBuyableItem<FlashlightItem>("Flashlight");
            // SpawnBuyableItem<FlashlightItem>("Pro-flashlight");
            // SpawnBuyableItem<StunGrenadeItem>("Stun grenade");
            // SpawnBuyableItem<PatcherTool>("Zap gun");
            SpawnBuyableItem<WalkieTalkie>("Walkie-talkie");

            // SpawnNonBuyableItem(["Laser pointer"]);
        }

        private static void SpawnShotgun()
        {
            var position = new Vector3(-1.4374f, 3.643f, -14.1965f);

            var level = StartOfRound.Instance.levels.First((level) => level.PlanetName == "8 Titan");
            var nutcracker = level.Enemies.Find((enemy) => enemy.enemyType.enemyName == "Nutcracker");
            var ncai = nutcracker.enemyType.enemyPrefab.gameObject.GetComponent<NutcrackerEnemyAI>();

            var shotgun = SpawnObject<ShotgunItem>(ncai.gunPrefab);
            shotgun.shellsLoaded = 500;
        }

        // client side only
        private static void ShowMeTheMoney(int amount)
        {
            var terminal = Object.FindObjectOfType<Terminal>();
            terminal.groupCredits = amount;
        }

        private static void SpawnNonBuyableItem(List<string> @itemNames)
        {
            foreach (var item in StartOfRound.Instance.allItemsList.itemsList) 
            {
                Logger.LogInfo("itemId:" + item.itemId + "-" + item.itemName);
                if (itemNames.Contains(item.itemName)) 
                {
                    SpawnObject<GrabbableObject>(item.spawnPrefab);
                }
            }
        }
        private static void SpawnBuyableItem<T>(string @itemName)
            where T : GrabbableObject
        {
            var terminal = Object.FindObjectOfType<Terminal>();
            var i = terminal.buyableItemsList.First((item) => item.itemName == @itemName);

            if (i != null)
            {
                SpawnObject<T>(i.spawnPrefab);
            }
        }

        private static T SpawnObject<T>(GameObject @object)
        where T : GrabbableObject
        {
            var gameObject = Object.Instantiate(@object, new Vector3(-1.4374f, 3.643f, -14.1965f), Quaternion.identity, RoundManager.Instance.spawnedScrapContainer);
            var component = gameObject.GetComponent<T>();
            component.transform.rotation = Quaternion.Euler(component.itemProperties.restingRotation);
            component.fallTime = 0f;
            component.scrapValue = 10;

            var netComponent = gameObject.GetComponent<NetworkObject>(); 
            netComponent.Spawn(false); 
            return component;
        }
    }
}
