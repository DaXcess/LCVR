using HarmonyLib;
using LCVR.Assets;
using LCVR.Networking;
using LCVR.Player;
using System.Collections;
using UnityEngine;

namespace LCVR.Physics.Interactions;

public class ChargeStation : MonoBehaviour, VRInteractable
{
    private InteractTrigger trigger;
    private ItemCharger charger;
    private Coroutine chargeItemCoroutine;

    public InteractableFlags Flags => InteractableFlags.RightHand;

    void Awake()
    {
        trigger = GetComponentInParent<InteractTrigger>();
        charger = GetComponentInParent<ItemCharger>();
    }

    public void OnColliderEnter(VRInteractor _)
    {
        if (!trigger.interactable || Plugin.Config.DisableChargeStationInteraction.Value)
            return;

        var item = VRSession.Instance.LocalPlayer.PlayerController.currentlyHeldObjectServer;
        if (item == null || !item.itemProperties.requiresBattery)
            return;

        charger.PlayChargeItemEffectServerRpc((int)VRSession.Instance.LocalPlayer.PlayerController.playerClientId);
        if (chargeItemCoroutine != null)
            StopCoroutine(chargeItemCoroutine);

        chargeItemCoroutine = StartCoroutine(chargeItemDelayed(item));
    }

    public void OnColliderExit(VRInteractor _)
    {
        if (chargeItemCoroutine != null)
        {
            charger.zapAudio.Stop();
            StopCoroutine(chargeItemCoroutine);

            DNet.CancelChargerAnimation();
        }
    }

    private IEnumerator chargeItemDelayed(GrabbableObject item)
    {
        charger.zapAudio.Play();
        yield return new WaitForSeconds(0.75f);

        charger.chargeStationAnimator.SetTrigger("zap");
        item.insertedBattery = new Battery(false, 1);
        item.SyncBatteryServerRpc(100);

        chargeItemCoroutine = null;
    }

    public void CancelChargingAnimation()
    {
        charger.zapAudio.Stop();
        charger.StopCoroutine(charger.chargeItemCoroutine);
    }

    public bool OnButtonPress(VRInteractor _) { return false; }
    public void OnButtonRelease(VRInteractor _) { }

    public static ChargeStation Create()
    {
        var charger = FindObjectOfType<ItemCharger>();
        charger.name = "ChargingStationTrigger";

        var interactable = Instantiate(AssetManager.Interactable, charger.gameObject.transform);
        var station = interactable.AddComponent<ChargeStation>();

        interactable.transform.localScale = Vector3.one * 0.7f;

        return station;
    }
}
