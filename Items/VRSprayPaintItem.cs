using GameNetcodeStuff;
using HarmonyLib;
using LCVR.Player;
using System.Reflection;

namespace LCVR.Items;

internal class VRSprayPaintItem : VRItem<SprayPaintItem>
{
    private static readonly FieldInfo timeSinceSwitchingSlotsField = AccessTools.Field(typeof(PlayerControllerB), "timeSinceSwitchingSlots");

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

        timeSinceSwitchingSlotsField.SetValue(player, 0f);
        player.currentlyHeldObjectServer.ItemInteractLeftRightOnClient(false);
    }

    protected override void OnUpdate()
    {
    }
}
