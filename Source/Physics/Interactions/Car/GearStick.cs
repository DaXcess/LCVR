using System.IO;
using HarmonyLib;
using LCVR.Assets;
using LCVR.Networking;
using LCVR.Patches;
using LCVR.Player;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LCVR.Physics.Interactions.Car;

public class GearStick : MonoBehaviour, VRInteractable
{
    public InteractableFlags Flags => InteractableFlags.BothHands | InteractableFlags.NotWhileHeld;
    
    private VehicleController vehicle;
    private Channel channel;
    private Transform container;

    private const float PARK_POSITION = 1.7551f;
    private const float REVERSE_POSITION = 1.66f;
    private const float DRIVE_POSITION = 1.5463f;

    private const int HANDS_ON_LEGS = 0;
    private const int HANDS_ON_WHEEL = 1;
    private const int HAND_NEAR_GEAR = 4;
    private const int HAND_ON_GEAR = 5;

    private bool isHeld;
    private bool isHeldByLocal;
    private Transform localHand;
    
    private void Awake()
    {
        vehicle = GetComponentInParent<VehicleController>();
        channel = VRSession.Instance.NetworkSystem.CreateChannel(ChannelType.VehicleGearStick, vehicle.NetworkObjectId);
        
        container = transform.parent.parent;
        
        channel.OnPacketReceived += OnPacketReceived;
    }

    private void Update()
    {
        if (!isHeldByLocal || !localHand)
            return;

        var localPosition = container.InverseTransformPoint(localHand.position).z + 0.1f;
        
        if (vehicle.gear != CarGearShift.Park && Mathf.Abs(localPosition - PARK_POSITION) < 0.05f)
        {
            vehicle.ShiftToGearAndSync((int)CarGearShift.Park);
        }
        else if (vehicle.gear != CarGearShift.Reverse && Mathf.Abs(localPosition - REVERSE_POSITION) < 0.05f)
        {
            vehicle.ShiftToGearAndSync((int)CarGearShift.Reverse);
        }
        else if (vehicle.gear != CarGearShift.Drive && Mathf.Abs(localPosition - DRIVE_POSITION) < 0.05f)
        {
            vehicle.ShiftToGearAndSync((int)CarGearShift.Drive);
        }
    }

    private void OnDestroy()
    {
        channel.Dispose();
    }

    public void OnColliderEnter(VRInteractor interactor)
    {
        if (vehicle.currentDriver != VRSession.Instance.LocalPlayer.PlayerController)
            return;
        
        vehicle.currentDriver.playerBodyAnimator.SetInteger("SA_CarAnim", HAND_NEAR_GEAR);
    }

    public void OnColliderExit(VRInteractor interactor)
    {
        if (vehicle.currentDriver != VRSession.Instance.LocalPlayer.PlayerController)
            return;

        vehicle.currentDriver.playerBodyAnimator.SetInteger("SA_CarAnim",
            vehicle.ignitionStarted ? HANDS_ON_WHEEL : HANDS_ON_LEGS);
    }

    public bool OnButtonPress(VRInteractor interactor)
    {
        if (Plugin.Config.DisableCarGearStickInteractions.Value)
            return false;
        
        if (isHeld)
            return false;

        isHeld = true;
        isHeldByLocal = true;
        localHand = interactor.IsRightHand
            ? VRSession.Instance.LocalPlayer.RightHandVRTarget
            : VRSession.Instance.LocalPlayer.LeftHandVRTarget;

        interactor.SnapTo(transform.parent, new Vector3(0.1f, -0.03f, 0.2f),
            new Vector3(interactor.IsRightHand ? 90 : -90, 180, 270));
        interactor.FingerCurler.ForceFist(true);
        
        channel.SendPacket([(byte)GearStickCommand.GrabStick, interactor.IsRightHand ? (byte)1 : (byte)0]);
        
        if (vehicle.currentDriver == VRSession.Instance.LocalPlayer.PlayerController)
            vehicle.currentDriver.playerBodyAnimator.SetInteger("SA_CarAnim", HAND_ON_GEAR);

        return true;
    }

    public void OnButtonRelease(VRInteractor interactor)
    {
        if (!isHeldByLocal)
            return;
        
        isHeld = false;
        isHeldByLocal = false;
        localHand = null;

        interactor.SnapTo(null);
        interactor.FingerCurler.ForceFist(false);
        
        channel.SendPacket([(byte)GearStickCommand.ReleaseStick, interactor.IsRightHand ? (byte)1 : (byte)0]);
        
        if (vehicle.currentDriver == VRSession.Instance.LocalPlayer.PlayerController)
            vehicle.currentDriver.playerBodyAnimator.SetInteger("SA_CarAnim", HAND_NEAR_GEAR);
    }
    
    private void OnPacketReceived(ushort sender, BinaryReader reader)
    {
        switch ((GearStickCommand)reader.ReadByte())
        {
            case GearStickCommand.GrabStick:
            {
                // Discard packet if already held
                if (isHeld)
                    break;

                // Check if player exists
                if (!VRSession.Instance.NetworkSystem.TryGetPlayer(sender, out var player))
                    break;

                isHeld = true;

                var isRightHand = reader.ReadBoolean();
                if (isRightHand)
                {
                    player.RightFingerCurler.ForceFist(true);
                    player.SnapRightHandTo(transform.parent, new Vector3(0.1f, -0.03f, 0.2f),
                        new Vector3(90, 180, 270));
                }
                else
                {
                    player.LeftFingerCurler.ForceFist(true);
                    player.SnapLeftHandTo(transform.parent, new Vector3(0.1f, -0.03f, 0.2f),
                        new Vector3(-90, 180, 270));
                }
            }
            break;

            case GearStickCommand.ReleaseStick:
            {
                // Discard packet if not held by other
                if (!isHeld || isHeldByLocal)
                    break;

                // Check if player exists
                if (!VRSession.Instance.NetworkSystem.TryGetPlayer(sender, out var player))
                    break;

                isHeld = false;
                
                var isRightHand = reader.ReadBoolean();
                if (isRightHand)
                {
                    player.RightFingerCurler.ForceFist(false);
                    player.SnapRightHandTo(null);
                }
                else
                {
                    player.LeftFingerCurler.ForceFist(false);
                    player.SnapLeftHandTo(null);
                }
            }
            break;
        }
    }

    private enum GearStickCommand : byte
    {
        GrabStick,
        ReleaseStick
    }
}

[LCVRPatch(LCVRPatchTarget.Universal)]
[HarmonyPatch]
internal static class GearStickPatches
{
    [HarmonyPatch(typeof(VehicleController), nameof(VehicleController.Awake))]
    [HarmonyPostfix]
    private static void OnCarCreated(VehicleController __instance)
    {
        var gearStickObj = __instance.transform.Find("Meshes/GearStickContainer/GearStick");
        var gearStickInteractable = Object.Instantiate(AssetManager.Interactable, gearStickObj);

        gearStickInteractable.transform.localPosition = new Vector3(0, 0, 0.18f);
        gearStickInteractable.transform.localScale = new Vector3(0.1f, 0.1f, 0.2f);

        gearStickInteractable.AddComponent<GearStick>();
    }
}