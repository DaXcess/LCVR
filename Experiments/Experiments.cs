using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace LCVR.Experiments
{
    internal class Experiments
    {
        public static void SpawnStuffInShip()
        {
            var position = new Vector3(-1.4374f, 3.643f, -14.1965f);

            var level = StartOfRound.Instance.levels.First((level) => level.PlanetName == "8 Titan");
            var nutcracker = level.Enemies.Find((enemy) => enemy.enemyType.enemyName == "Nutcracker");
            var ncai = nutcracker.enemyType.enemyPrefab.gameObject.GetComponent<NutcrackerEnemyAI>();

            SpawnObject(ncai.gunPrefab);

            var terminal = GameObject.FindObjectOfType<Terminal>();
            var flashlight = terminal.buyableItemsList.First((item) => item.itemName == "Pro-flashlight");

            SpawnObject(flashlight.spawnPrefab);

            //foreach (var enemy in level.Enemies)
            //{
            //Logger.LogDebug(enemy.enemyType.enemyName);
            //}
        }

        private static void SpawnObject(GameObject @object)
        {
            var gameObject = GameObject.Instantiate(@object, new Vector3(-1.4374f, 3.643f, -14.1965f), Quaternion.identity, RoundManager.Instance.spawnedScrapContainer);
            var component = gameObject.GetComponent<GrabbableObject>();
            component.transform.rotation = Quaternion.Euler(component.itemProperties.restingRotation);
            component.fallTime = 0f;
            component.scrapValue = 10;

            var netComponent = gameObject.GetComponent<NetworkObject>();
            netComponent.Spawn(false);
        }
    }
}
