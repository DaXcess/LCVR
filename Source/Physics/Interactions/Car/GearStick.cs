using HarmonyLib;
using LCVR.Assets;
using LCVR.Patches;
using LCVR.Player;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LCVR.Physics.Interactions.Car;

public class GearStick : MonoBehaviour, VRInteractable
{
    public InteractableFlags Flags => InteractableFlags.BothHands | InteractableFlags.NotWhileHeld;

    // TODO: Remove
    private Transform cube;
    
    private VehicleController vehicle;
    private Transform container;

    private const float PARK_POSITION = 1.7551f;
    private const float REVERSE_POSITION = 1.66f;
    private const float DRIVE_POSITION = 1.5463f;

    private bool isHeld;
    private bool isHeldByLocal;
    private Transform localHand;
    
    private void Awake()
    {
        vehicle = GetComponentInParent<VehicleController>();
        
        container = transform.parent.parent;

        cube = Instantiate(AssetManager.Interactable, container).transform;
        cube.localScale = Vector3.one * 0.1f;
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

        cube.localPosition = new Vector3(-0.125f, 0.1f, localPosition);
    }

    public bool OnButtonPress(VRInteractor interactor)
    {
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
    }
    
    public void OnColliderEnter(VRInteractor interactor) { }
    public void OnColliderExit(VRInteractor interactor) { }
}

[LCVRPatch(LCVRPatchTarget.Universal)]
[HarmonyPatch]
internal static class GearStickPatches
{
    [HarmonyPatch(typeof(VehicleController), nameof(VehicleController.Awake))]
    [HarmonyPostfix]
    private static void OnCarCreated(VehicleController __instance)
    {
        if (false)
            return;

        var gearStickObj = __instance.transform.Find("Meshes/GearStickContainer/GearStick");
        var gearStickInteractable = Object.Instantiate(AssetManager.Interactable, gearStickObj);

        gearStickInteractable.transform.localPosition = new Vector3(0, 0, 0.18f);
        gearStickInteractable.transform.localScale = new Vector3(0.1f, 0.1f, 0.2f);

        gearStickInteractable.AddComponent<GearStick>();
    }
}