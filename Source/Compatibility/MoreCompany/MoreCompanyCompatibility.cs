using MoreCompany.Cosmetics;
using UnityEngine;

namespace LCVR.Compatibility.MoreCompany;

internal static class MoreCompanyCompatibility
{
    private static GameObject canvasUi;
    private static Transform previousParent;
    private static Vector3 previousPosition;
    private static Vector3 previousRotation;
    private static Vector3 previousScale;
    private static Vector3 previousButtonPosition;

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

        previousParent = canvasUi.transform.parent;
        previousPosition = canvasUi.transform.localPosition;
        previousRotation = canvasUi.transform.localEulerAngles;
        previousScale = canvasUi.transform.localScale;
        
        canvasUi.transform.parent = menuContainer.transform;
        canvasUi.transform.localPosition = new Vector3(-46, 6, -90);
        canvasUi.transform.localEulerAngles = Vector3.zero;
        canvasUi.transform.localScale = Vector3.one;

        var activateButton = canvasUi.Find("ActivateButton");
        
        previousButtonPosition = activateButton.transform.localPosition;
        
        activateButton.transform.localPosition = new Vector3(activateButton.transform.localPosition.x,
            activateButton.transform.localPosition.y, 90);

        overlay.Find("CanvasCam").SetActive(false);
    }

    public static void RevertMoreCompanyUIMainMenu()
    {
        if (NoCosmetics)
            return;
        
        var overlay = GameObject.Find("TestOverlay(Clone)");
        if (overlay == null)
            return;

        canvasUi.transform.parent = previousParent;
        canvasUi.transform.localPosition = previousPosition;
        canvasUi.transform.localEulerAngles = previousRotation;
        canvasUi.transform.localScale = previousScale;

        var activateButton = canvasUi.Find("ActivateButton");
        activateButton.transform.localPosition = previousButtonPosition;

        overlay.Find("CanvasCam").SetActive(true);
    }

    public static void SetupMoreCompanyUIInGame()
    {
        if (NoCosmetics)
            return;
        
        var canvasUi = GameObject.Find("Systems/UI/Canvas/GlobalScale");

        canvasUi.transform.localPosition = new Vector3(0, 0, -90);
    }
}