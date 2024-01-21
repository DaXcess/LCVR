using HarmonyLib;
using LCVR.Assets;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LCVR
{
    internal class ObjectScanner
    {
        private readonly Camera mainCamera;

        internal ObjectScanner(Camera mainCamera)
        {
            this.mainCamera = mainCamera;

            foreach (Transform child in GameObject.Find("ObjectScanner").transform)
            {
                var canvas = child.gameObject.AddComponent<Canvas>();
                canvas.worldCamera = mainCamera;
                canvas.renderMode = RenderMode.WorldSpace;

                // Render all object scanner stuff on top
                child.GetComponentsInChildren<Image>().Do(image => image.material = AssetManager.alwaysOnTopMat);
                child.GetComponentsInChildren<TextMeshProUGUI>().Do(text =>
                {
                    text.fontSharedMaterial = new Material(text.fontSharedMaterial);
                    text.isOverlay = true;
                });
            }
        }

        private Dictionary<RectTransform, ScanNodeProperties> scanNodes
        {
            get
            {
                return (Dictionary<RectTransform, ScanNodeProperties>)typeof(HUDManager).GetField("scanNodes", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(HUDManager.Instance);
            }
        }

        public void Update()
        {
            var manager = HUDManager.Instance;

            for (var i = 0; i < manager.scanElements.Length; i++)
            {
                if (scanNodes.Count > 0 && scanNodes.TryGetValue(manager.scanElements[i], out var scanNodeProps) && scanNodeProps != null)
                {
                    if (InvokeFunction<bool>("NodeIsNotVisible", scanNodeProps, i)) continue;

                    var scanObject = manager.scanElements[i].gameObject;
                    scanObject.transform.position = scanNodeProps.transform.position;

                    // TODO: Check how to properly calculate scale based on player position to always give the illusion
                    // of having the same size no matter the distance since it is now a 3D world positioned canvas
                    // Dev note: Meh this is fine tbh
                    var nominalDistance = 5;

                    var distance = Vector3.Distance(mainCamera.transform.position, scanNodeProps.transform.position);
                    var scaleFactor = distance / nominalDistance;

                    scanObject.transform.rotation = Quaternion.LookRotation(scanObject.transform.position - mainCamera.transform.position);
                    scanObject.transform.position += scanObject.transform.forward * -0.2f;
                    scanObject.transform.localScale = Vector3.one * scaleFactor;
                }
            }
        }

        private static T InvokeFunction<T>(string name, params object[] args)
        {
            return (T)typeof(HUDManager).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(HUDManager.Instance, args);
        }
    }
}
