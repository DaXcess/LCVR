using UnityEngine;

namespace LCVR.Compatibility
{
    internal class MoreCompany
    {
        public static void SetupMoreCompanyUI()
        {
            var overlay = GameObject.Find("TestOverlay(Clone)");
            var menuContainer = GameObject.Find("MenuContainer");

            if (overlay == null)
                return;

            var canvasUi = overlay.Find("Canvas/GlobalScale");
            canvasUi.transform.parent = menuContainer.transform;
            canvasUi.transform.localPosition = new Vector3(-46, 6, -90);
            canvasUi.transform.localEulerAngles = Vector3.zero;
            canvasUi.transform.localScale = Vector3.one;

            var activateButton = canvasUi.Find("ActivateButton");
            activateButton.transform.localPosition = new Vector3(activateButton.transform.localPosition.x, activateButton.transform.localPosition.y, 90);

            overlay.Find("CanvasCam").SetActive(false);
        }
    }
}
