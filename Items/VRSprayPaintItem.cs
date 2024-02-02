using GameNetcodeStuff;
using HarmonyLib;
using LCVR.Player;
using System.Reflection;

namespace LCVR.Items
{
    internal class VRSprayPaintItem : VRItem<SprayPaintItem>
    {
        private static readonly FieldInfo timeSinceSwitchingSlotsField = AccessTools.Field(typeof(PlayerControllerB), "timeSinceSwitchingSlots");

        private VRController hand;

        private new void Awake()
        {
            base.Awake();

            if (!IsLocal)
                return;

            hand = VRPlayer.Instance.mainHand;
            hand.motionDetector.onShake.AddListener(OnShakeMotion);

            if (!Plugin.Config.SprayPaintTipSeen.Value)
            {
                HUDManager.Instance.DisplayTip("Shake shake shake", "You can shake the can by shaking your hand back on forth in quick succession.");
                Plugin.Config.SprayPaintTipSeen.Value = true;
            }
        }

        private void OnDestroy()
        {
            if (IsLocal)
                hand.motionDetector.onShake.RemoveListener(OnShakeMotion);
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
}
