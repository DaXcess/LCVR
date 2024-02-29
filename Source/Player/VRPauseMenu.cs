using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace LCVR.Player;

internal class VRPauseMenu : MonoBehaviour
{
    private XRRayInteractor leftController;
    private XRRayInteractor rightController;

    void Awake()
    {
        leftController = new GameObject("LController").CreateInteractorController(Utils.Hand.Left);
        rightController = new GameObject("RController").CreateInteractorController(Utils.Hand.Right);

        leftController.transform.parent = transform;
        rightController.transform.parent = transform;

        leftController.rayOriginTransform.localRotation = Quaternion.Euler(60, 347, 90);
        rightController.rayOriginTransform.localRotation = Quaternion.Euler(60, 347, 270);
    }
}
