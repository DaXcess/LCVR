using System.Linq;
using HarmonyLib;
using LCVR.Assets;
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
    
    private float currentRotation;
    private float pendingRotation;
    private float velocity;
    
    private int handsOnWheel;

    internal SteeringWheelSnapPoint[] snapPoints;
    
    private void Awake()
    {
        vehicle = GetComponentInParent<VehicleController>();
        wheelAnimator = GetComponentInParent<Animator>();
    }

    private void Update()
    {
        // TODO: Networking
        if ((VRSession.InVR && vehicle.localPlayerInControl) || false)
            wheelAnimator.enabled = false;
        else
            wheelAnimator.enabled = true;
    }

    private void LateUpdate()
    {
        try
        {
            if (Mathf.Abs(pendingRotation) < 1)
                return;

            currentRotation = Mathf.SmoothDampAngle(currentRotation, currentRotation + pendingRotation, ref velocity,
                SMOOTH_TIME, Mathf.Max(ROTATION_SPEED * handsOnWheel, 0.25f));
            currentRotation = Mathf.Clamp(currentRotation, -MAX_ROTATION, MAX_ROTATION);
            
            Logger.LogDebug(velocity);

            pendingRotation = Mathf.Lerp(pendingRotation, 0, Time.deltaTime * ROTATION_SPEED * handsOnWheel);

            transform.localEulerAngles = new Vector3(0, 0, currentRotation);
        }
        finally
        {
            handsOnWheel = 0;
        }
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
        
        vehicle.drivePedalPressed = vehicle.moveInputVector.y > 0.1f;
        vehicle.brakePedalPressed = vehicle.moveInputVector.y < -0.1f;
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
        if (vehicle.carDestroyed || !vehicle.localPlayerInControl || !vehicle.ignitionStarted)
            return false;
        
        interactor.FingerCurler.ForceFist(true);
        interactor.SnapTo(transform.parent, new Vector3(0, -0.4f, -0.1f), new Vector3(0, 180, 0));
        handTransform = interactor.IsRightHand
            ? VRSession.Instance.LocalPlayer.RightHandVRTarget
            : VRSession.Instance.LocalPlayer.LeftHandVRTarget;

        if (interactor.IsRightHand)
            VRSession.Instance.LocalPlayer.PrimaryController.enabled = false;
        
        return true;
    }

    public void OnButtonRelease(VRInteractor interactor)
    {
        interactor.FingerCurler.ForceFist(false);
        interactor.SnapTo(null);
        handTransform = null;
        
        if (interactor.IsRightHand)
            VRSession.Instance.LocalPlayer.PrimaryController.enabled = true;
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