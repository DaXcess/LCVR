using System.Runtime.CompilerServices;
using GameNetcodeStuff;
using MoreCompany.Cosmetics;
using UnityEngine;

namespace LCVR.Compatibility.MoreCompany;

internal static class MoreCompanyCompatibility
{
    private static GameObject canvasUi;

    private static bool NoCosmetics => CosmeticRegistry.cosmeticInstances.Count == 0;
    
    public static void SetupMoreCompanyUIMainMenu()
    {
        if (NoCosmetics)
            return;
        
        var overlay = GameObject.Find("TestOverlay(Clone)");
        var menuContainer = GameObject.Find("MenuContainer");

        if (overlay == null)
            return;

        canvasUi = overlay.Find("Canvas/GlobalScale");
        
        canvasUi.transform.parent = menuContainer.transform;
        canvasUi.transform.localPosition = new Vector3(-46, 6, -90);
        canvasUi.transform.localEulerAngles = Vector3.zero;
        canvasUi.transform.localScale = Vector3.one;

        var activateButton = canvasUi.Find("ActivateButton");
        activateButton.transform.localPosition = new Vector3(activateButton.transform.localPosition.x,
            activateButton.transform.localPosition.y, 90);

        overlay.Find("CanvasCam").SetActive(false);
    }

    public static void SetupMoreCompanyUIInGame()
    {
        if (NoCosmetics)
            return;
        
        var canvasUi = GameObject.Find("Systems/UI/Canvas/GlobalScale");

        canvasUi.transform.localPosition = new Vector3(0, 0, -90);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void EnablePlayerCosmetics(PlayerControllerB player, bool enabled)
    {
        player.transform.Find("ScavengerModel/metarig").GetComponent<CosmeticApplication>().enabled = enabled;
    }
}