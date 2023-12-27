using GameNetcodeStuff;
using LCVR.Player;
using System.Reflection;
using SprayPaint = SprayPaintItem;

namespace LCVR.Items
{
    internal class SprayPaintItem : VRItem<SprayPaint>
    {
        private VRController hand;

        private new void Awake()
        {
            base.Awake();

            hand = VRPlayer.Instance.mainHand;
            hand.motionDetector.onShake.AddListener(OnShakeMotion);
        }

        private void OnDestroy()
        {
            hand.motionDetector.onShake.RemoveListener(OnShakeMotion);
        }

        private void OnShakeMotion()
        {
            if (player.currentlyHeldObjectServer != item)
                return;

            if (player.isGrabbingObjectAnimation || player.inTerminalMenu || player.inSpecialInteractAnimation)
                return;

            typeof(PlayerControllerB).GetField("timeSinceSwitchingSlots", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, 0f);
            player.currentlyHeldObjectServer.ItemInteractLeftRightOnClient(false);
        }

        protected override void OnUpdate()
        {
        }
    }
}
