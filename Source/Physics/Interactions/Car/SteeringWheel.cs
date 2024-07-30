using System;
using System.IO;
using System.Linq;
using HarmonyLib;
using LCVR.Assets;
using LCVR.Networking;
using LCVR.Patches;
using LCVR.Player;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LCVR.Physics.Interactions.Car;

public class SteeringWheel : MonoBehaviour
{
    private const float ROTATION_SPEED = 600;
    private const float MAX_ROTATION = 480;
    private const float MIN_ROTATION = 5;
    private const float SMOOTH_TIME = 0.1f;

    private VehicleController vehicle;
    private Animator wheelAnimator;
    private Channel channel;
    
    private float currentRotation;
    private float pendingRotation;
    private float velocity;
    
    private int handsOnWheel;
    private int packetCounter;

    internal SteeringWheelSnapPoint[] snapPoints;

    private VRNetPlayer OtherDriver =>
        DNet.Players.FirstOrDefault(player => player.PlayerController == vehicle.currentDriver);

    private bool ControlledByLocal => VRSession.InVR && vehicle.localPlayerInControl &&
                                      !Plugin.Config.DisableCarSteeringWheelInteraction.Value;

    private bool ControlledByOther => vehicle.currentDriver is not null &&
                                      DNet.Players.Any(player =>
                                          !player.AdditionalData.DisableSteeringWheel &&
                                          player.PlayerController == vehicle.currentDriver);
    
    private void Awake()
    {
        vehicle = GetComponentInParent<VehicleController>();
        wheelAnimator = GetComponentInParent<Animator>();

        channel = DNet.CreateChannel(ChannelType.VehicleSteeringWheel, vehicle.NetworkObjectId);
        channel.OnPacketReceived += OnPacketReceived;
    }

    private void Update()
    {
        wheelAnimator.enabled = !ControlledByLocal && !ControlledByOther;
    }

    private void LateUpdate()
    {
        if (!ControlledByLocal && !ControlledByOther)
            return;
            
        try
        {
            if (ControlledByLocal)
            {
                packetCounter++;

                if (packetCounter % 2 == 0)
                    channel.SendPacket([
                        (byte)SteeringWheelCommand.Sync, ..Serialization.Serialize(new SteeringWheelSync
                        {
                            currentRotation = currentRotation,
                            pendingRotation = pendingRotation,
                            velocity = velocity,
                            handsOnWheel = handsOnWheel
                        })
                    ]);
            }

            if (Mathf.Abs(pendingRotation) < 1)
                return;

            currentRotation = Mathf.SmoothDampAngle(currentRotation, currentRotation + pendingRotation, ref velocity,
                SMOOTH_TIME, Mathf.Max(ROTATION_SPEED * handsOnWheel, 0.25f));
            currentRotation = Mathf.Clamp(currentRotation, -MAX_ROTATION, MAX_ROTATION);
            pendingRotation = Mathf.Lerp(pendingRotation, 0, Time.deltaTime * ROTATION_SPEED * handsOnWheel);

            transform.localEulerAngles = new Vector3(0, 0, currentRotation);
        }
        finally
        {
            handsOnWheel = 0;
        }
    }

    private void OnDestroy()
    {
        channel.Dispose();
    }

    public void ApplyRotationAtPointTowards(Vector3 point, Vector3 target)
    {
        handsOnWheel++;
        
        var targetPosition = transform.InverseTransformPoint(target);
        var pointPosition = transform.InverseTransformPoint(point);

        targetPosition.z = 0;
        pointPosition.z = 0;
        
        var angle = Vector3.SignedAngle(pointPosition, targetPosition, Vector3.forward);

        pendingRotation += angle;
    }

    internal void GetSteeringInput()
    {
        const float div = MAX_ROTATION / 3;

        if (!vehicle.localPlayerInControl)
            return;
        
        vehicle.moveInputVector =
            IngamePlayerSettings.Instance.playerInput.actions.FindAction("Move").ReadValue<Vector2>();

        if (Mathf.Abs(currentRotation) < MIN_ROTATION)
            vehicle.steeringInput = 0;
        else
            vehicle.steeringInput = Mathf.Clamp(currentRotation / div, -3f, 3f);

        vehicle.steeringAnimValue = Mathf.Abs(velocity) < 20 ? 0 : Mathf.Sign(velocity);
        vehicle.drivePedalPressed = vehicle.moveInputVector.y > 0.1f;
        vehicle.brakePedalPressed = vehicle.moveInputVector.y < -0.1f;
    }

    /// <summary>
    /// Notify other players that the steering wheel was grabbed
    /// </summary>
    internal void HandAttachedToWheel(bool isRightHand, int snapPointIndex)
    {
        channel.SendPacket([
            (byte)SteeringWheelCommand.Hand, ..Serialization.Serialize(new HandOnWheel
            {
                isOnWheel = true,
                isRightHand = isRightHand,
                snapPointIndex = snapPointIndex
            })
        ]);
    }
    
    /// <summary>
    /// Notify other players that the steering wheel was released
    /// </summary>
    internal void HandDetachedFromWheel(bool isRightHand)
    {
        channel.SendPacket([
            (byte)SteeringWheelCommand.Hand, ..Serialization.Serialize(new HandOnWheel
            {
                isOnWheel = false,
                isRightHand = isRightHand,
                snapPointIndex = 0
            })
        ]);
    }

    private void OnPacketReceived(ushort sender, BinaryReader reader)
    {
        switch ((SteeringWheelCommand)reader.ReadByte())
        {
            case SteeringWheelCommand.Sync:
                // Only allow sync if sender is the driver of the vehicle
                if (OtherDriver?.PlayerController.playerClientId != sender)
                    break;

                var sync = Serialization.Deserialize<SteeringWheelSync>(reader);
                
                currentRotation = sync.currentRotation;
                pendingRotation = sync.pendingRotation;
                velocity = sync.velocity;
                handsOnWheel = sync.handsOnWheel;

                break;

            case SteeringWheelCommand.Hand:
                // Only allow sync if sender is the driver of the vehicle
                if (OtherDriver?.PlayerController.playerClientId != sender)
                    break;  

                var handOnWheel = Serialization.Deserialize<HandOnWheel>(reader);

                if (!handOnWheel.isOnWheel)
                {
                    if (handOnWheel.isRightHand)
                        OtherDriver.SnapRightHandTo(null);
                    else
                        OtherDriver.SnapLeftHandTo(null);

                    break;
                }

                var target = snapPoints[handOnWheel.snapPointIndex];

                if (handOnWheel.isRightHand)
                    OtherDriver.SnapRightHandTo(target.transform, new Vector3(0, -0.4f, -0.1f), new Vector3(0, 180, 0));
                else
                    OtherDriver.SnapLeftHandTo(target.transform, new Vector3(0, -0.4f, -0.1f), new Vector3(0, 180, 0));

                break;
            
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private enum SteeringWheelCommand : byte
    {
        Sync,
        Hand,
    }

    [Serialize]
    private struct SteeringWheelSync
    {
        public float currentRotation;
        public float pendingRotation;
        public float velocity;
        public int handsOnWheel;
    }

    [Serialize]
    private struct HandOnWheel
    {
        public bool isOnWheel;
        public bool isRightHand;
        public int snapPointIndex;
    }
}

public class SteeringWheelSnapPoint : MonoBehaviour, VRInteractable
{
    private VehicleController vehicle;
    private SteeringWheel steeringWheel;
    private Transform handTransform;

    internal int pointIndex;
    
    public InteractableFlags Flags => InteractableFlags.BothHands;

    private void Awake()
    {
        vehicle = GetComponentInParent<VehicleController>();
        steeringWheel = GetComponentInParent<SteeringWheel>();
    }

    private void Update()
    {
        if (handTransform is null || !vehicle.localPlayerInControl || !vehicle.ignitionStarted)
            return;

        steeringWheel.ApplyRotationAtPointTowards(transform.position, handTransform.position);
    }

    public bool OnButtonPress(VRInteractor interactor)
    {
        if (Plugin.Config.DisableCarSteeringWheelInteraction.Value)
            return false;
        
        if (vehicle.carDestroyed || !vehicle.localPlayerInControl || !vehicle.ignitionStarted)
            return false;
        
        interactor.FingerCurler.ForceFist(true);
        interactor.SnapTo(transform.parent, new Vector3(0, -0.4f, -0.1f), new Vector3(0, 180, 0));
        handTransform = interactor.IsRightHand
            ? VRSession.Instance.LocalPlayer.RightHandVRTarget
            : VRSession.Instance.LocalPlayer.LeftHandVRTarget;

        if (interactor.IsRightHand)
            VRSession.Instance.LocalPlayer.PrimaryController.enabled = false;

        steeringWheel.HandAttachedToWheel(interactor.IsRightHand, pointIndex);
        
        return true;
    }

    public void OnButtonRelease(VRInteractor interactor)
    {
        interactor.FingerCurler.ForceFist(false);
        interactor.SnapTo(null);
        handTransform = null;
        
        if (interactor.IsRightHand)
            VRSession.Instance.LocalPlayer.PrimaryController.enabled = true;
        
        steeringWheel.HandDetachedFromWheel(interactor.IsRightHand);
    }
    
    public void OnColliderEnter(VRInteractor _) { }
    public void OnColliderExit(VRInteractor _) { }
}

[LCVRPatch(LCVRPatchTarget.Universal)]
[HarmonyPatch]
internal static class SteeringWheelPatches
{
    [HarmonyPatch(typeof(VehicleController), nameof(VehicleController.Awake))]
    [HarmonyPostfix]
    private static void OnCarCreated(VehicleController __instance)
    {
        var steeringWheel = __instance.transform.Find("Meshes/SteeringWheelContainer/SteeringWheel");
        var points = Object.Instantiate(AssetManager.SteeringWheelPoints, steeringWheel);

        var wheel = steeringWheel.gameObject.AddComponent<SteeringWheel>();
        var snapPoints = points.GetComponentsInChildren<BoxCollider>()
            .Select((point, i) =>
            {
                var snapPoint = point.gameObject.AddComponent<SteeringWheelSnapPoint>();
                snapPoint.pointIndex = i;
                
                return snapPoint;
            }).ToArray();

        wheel.snapPoints = snapPoints;
    }
}