using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LCVR.Assets;

public static class StaticAssets
{
    private static readonly Dictionary<string, GameObject> RootObjects = [];

    static StaticAssets()
    {
        PopulateStaticRootObjects();

        SceneManager.sceneLoaded += (_, _) => PopulateStaticRootObjects();
    }

    private static void PopulateStaticRootObjects()
    {
        RootObjects.Clear();

        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            if (go.transform.parent == null && !go.scene.IsValid())
                RootObjects.TryAdd(go.name, go);
    }

    public static bool TryGetObject(string name, out GameObject go)
    {
        return RootObjects.TryGetValue(name, out go);
    }

    public static T GetRootComponent<T>(string name)
        where T : Component
    {
        return !TryGetObject(name, out var go) ? null : go.GetComponent<T>();
    }
}