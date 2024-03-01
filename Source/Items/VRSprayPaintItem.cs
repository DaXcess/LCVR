using LCVR.Player;

namespace LCVR.Items;

internal class VRSprayPaintItem : VRItem<SprayPaintItem>
{
    private new void Awake()
    {
        base.Awake();

        if (!IsLocal)
            return;

        VRSession.Instance.MotionDetector.onShake.AddListener(OnShakeMotion);
    }

    private void OnDestroy()
    {
        if (IsLocal)
            VRSession.Instance.MotionDetector.onShake.RemoveListener(OnShakeMotion);
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
    }
}
