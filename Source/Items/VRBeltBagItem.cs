using LCVR.Input;
using LCVR.Player;
using UnityEngine;
using UnityEngine.XR;

namespace LCVR.Items;

public class VRBeltBagItem : VRItem<BeltBagItem>
{
    private ShakeDetector shake;
    private ShakeDetector shakeBig;
    private BeltBagInventoryUI inventoryUI;

    private float lastShakeTime;
    private float lastItemDrop;
    private float minWaitTime = 0.3f;
    
    protected override void Awake()
    {
        base.Awake();

        shake = new ShakeDetector(transform, 0.06f, true);
        shakeBig = new ShakeDetector(transform, 0.1f, true);
        inventoryUI = FindObjectOfType<BeltBagInventoryUI>(true);
        
        shake.onShake += () =>
        {
            if (Time.realtimeSinceStartup - lastShakeTime > 0.5f)
                return;
            
            if (item.currentPlayerChecking != StartOfRound.Instance.localPlayerController)
                return;
            
            if (transform.forward.y > -0.6)
                return;

            if (Time.realtimeSinceStartup - lastItemDrop < minWaitTime)
                return;

            if (item.objectsInBag.Count < 1)
                return;
            
            inventoryUI.RemoveItemFromUI(item.objectsInBag.Count - 1);
            lastItemDrop = Time.realtimeSinceStartup;
            lastShakeTime = Time.realtimeSinceStartup;
            minWaitTime = Random.Range(0.1f, 0.3f);
            
            RoundManager.PlayRandomClip(item.bagAudio, item.grabItemInBagSFX, audibleNoiseID: -1);
            VRSession.VibrateController(XRNode.RightHand, 0.1f, 0.5f);
        };

        shakeBig.onShake += () =>
        {
            lastShakeTime = Time.realtimeSinceStartup;
        };
    }

    protected override void OnUpdate()
    {
        shake.Update();
        shakeBig.Update();
    }
}