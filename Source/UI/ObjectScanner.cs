﻿using LCVR.Player;
using UnityEngine;

namespace LCVR.UI;

internal class ObjectScanner : MonoBehaviour
{
    private void Awake()
    {
        var scanner = GameObject.Find("ObjectScanner");
        
        foreach (Transform child in scanner.transform)
        {
            var canvas = child.gameObject.AddComponent<Canvas>();
            canvas.worldCamera = VRSession.Instance.MainCamera;
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 1;
        }
    }

    private void LateUpdate()
    {
        var manager = HUDManager.Instance;

        for (var i = 0; i < manager.scanElements.Length; i++)
        {
            if (manager.scanNodes.Count <= 0 ||
                !manager.scanNodes.TryGetValue(manager.scanElements[i], out var scanNodeProps) || scanNodeProps == null)
                continue;

            if (manager.NodeIsNotVisible(scanNodeProps, i)) continue;

            var scanObject = manager.scanElements[i].gameObject;
            scanObject.transform.position = scanNodeProps.transform.position;

            // Dev note: Meh this is fine tbh
            const int nominalDistance = 5;

            var distance = Vector3.Distance(VRSession.Instance.MainCamera.transform.position, scanNodeProps.transform.position);
            var scaleFactor = distance / nominalDistance;

            scanObject.transform.rotation = Quaternion.LookRotation(scanObject.transform.position - VRSession.Instance.MainCamera.transform.position);
            scanObject.transform.position += scanObject.transform.forward * -0.2f;
            scanObject.transform.localScale = Vector3.one * scaleFactor;
        }
    }
}
