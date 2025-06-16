using LCVR.Input;
using LCVR.Managers;

namespace LCVR.Items;

internal class VRSprayPaintItem : VRItem<SprayPaintItem>
{
    private ShakeDetector shake;
    
    private new void Awake()
    {
        base.Awake();

        if (!IsLocal)
            return;

        shake = new ShakeDetector(VRSession.Instance.LocalPlayer.PrimaryController.transform, 0.035f, true);
        shake.onShake += OnShakeMotion;
    }

    private void OnDestroy()
    {
        if (IsLocal)
            shake.onShake -= OnShakeMotion;
    }

    private void OnShakeMotion()
    {
        if (player.currentlyHeldObjectServer != item)
            return;

        if (player.isGrabbingObjectAnimation || player.inTerminalMenu || player.inSpecialInteractAnimation)
            return;

        player.timeSinceSwitchingSlots = 0f;
        player.currentlyHeldObjectServer.ItemInteractLeftRightOnClient(false);
    }

    protected override void OnUpdate()
    {
        if (!IsLocal)
            return;
        
        shake.Update();
    }
}
