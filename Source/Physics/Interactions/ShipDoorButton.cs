using LCVR.Assets;
using LCVR.Player;
using UnityEngine;

namespace LCVR.Physics.Interactions;

internal class ShipDoorButton : MonoBehaviour, VRInteractable
{
    private InteractTrigger trigger;
    private float lastInteractTime;

    public ShipDoorButton otherButton;

    public InteractableFlags Flags => InteractableFlags.BothHands;
    
    private bool CanInteract => trigger.interactable && Time.realtimeSinceStartup - lastInteractTime > 0.5f;

    private void Awake()
    {
        trigger = GetComponentInParent<InteractTrigger>();
        trigger.gameObject.name = "ShipDoorButtonInteractable";
    }

    public void OnColliderEnter(VRInteractor _)
    {
        if (!CanInteract || !otherButton.CanInteract)
            return;

        lastInteractTime = Time.realtimeSinceStartup;
        trigger.onInteract?.Invoke(VRSession.Instance.LocalPlayer.PlayerController);
    }

    public void OnColliderExit(VRInteractor _) { }
    public bool OnButtonPress(VRInteractor _) { return false; }
    public void OnButtonRelease(VRInteractor _) { }

    public static void Create()
    {
        if (Plugin.Config.DisableShipDoorInteraction.Value)
            return;

        var openDoorButtonObject = GameObject.Find("HangarDoorButtonPanel/StartButton");
        var closeDoorButtonObject = GameObject.Find("HangarDoorButtonPanel/StopButton");

        var openDoorInteractableObject = Instantiate(AssetManager.Interactable, openDoorButtonObject.transform.GetChild(0));
        var closeDoorInteractableObject = Instantiate(AssetManager.Interactable, closeDoorButtonObject.transform.GetChild(0));

        openDoorInteractableObject.transform.localPosition = new Vector3(-0.04f, 0, -0.11f);
        openDoorInteractableObject.transform.localScale = new Vector3(0.3f, 1, 0.5f);

        closeDoorInteractableObject.transform.localPosition = new Vector3(-0.02f, 0, 0.1f);
        closeDoorInteractableObject.transform.localScale = new Vector3(0.3f, 1, 0.5f);

        var openDoorInteractable = openDoorInteractableObject.AddComponent<ShipDoorButton>();
        var closeDoorInteractable = closeDoorInteractableObject.AddComponent<ShipDoorButton>();

        openDoorInteractable.otherButton = closeDoorInteractable;
        closeDoorInteractable.otherButton = openDoorInteractable;
    }
}
