using HarmonyLib;
using LCVR.Assets;
using LCVR.Patches;
using LCVR.Player;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LCVR.Physics.Interactions.Car;

public class StartIgnition : MonoBehaviour, VRInteractable
{
    internal VehicleController vehicle;
    private Transform snapPoint;
    
    public InteractableFlags Flags => InteractableFlags.RightHand | InteractableFlags.NotWhileHeld;
    public float LastInteractedWith { get; private set; }

    private void Awake()
    {
        snapPoint = transform.parent.parent.Find("CarKeyTurnedPos");
    }

    public void OnColliderEnter(VRInteractor interactor)
    {
        if (!vehicle.localPlayerInControl || vehicle.ignitionStarted)
            return;

        interactor.FingerCurler.Enabled = false;

        if (vehicle.keyIsInIgnition)
            return;
        
        vehicle.keyIsInDriverHand = true;
    }

    public void OnColliderExit(VRInteractor interactor)
    {
        interactor.FingerCurler.Enabled = true;
        vehicle.keyIsInDriverHand = false;
    }

    public bool OnButtonPress(VRInteractor interactor)
    {
        if (!vehicle.localPlayerInControl || vehicle.ignitionStarted)
            return false;
        
        interactor.SnapTo(snapPoint, new Vector3(1, 1, 0), new Vector3(0, 0, 140));

        vehicle.StartTryCarIgnition();
        LastInteractedWith = Time.realtimeSinceStartup;
        
        return true;
    }

    public void OnButtonRelease(VRInteractor interactor)
    {
        interactor.SnapTo(null);
        
        vehicle.CancelTryCarIgnition();
    }
}

public class StopIgnition : MonoBehaviour, VRInteractable
{
    public InteractableFlags Flags => InteractableFlags.RightHand | InteractableFlags.NotWhileHeld;
    
    internal StartIgnition startIgnition;
    internal VehicleController vehicle;
    
    public bool OnButtonPress(VRInteractor interactor)
    {
        if (!vehicle.localPlayerInControl)
            return false;

        if (Time.realtimeSinceStartup - startIgnition.LastInteractedWith < 1f)
            return true;
        
        vehicle.RemoveKeyFromIgnition();
        
        return true;
    }

    public void OnColliderEnter(VRInteractor interactor) { }
    public void OnColliderExit(VRInteractor interactor) { }
    public void OnButtonRelease(VRInteractor interactor) { }
}

[LCVRPatch(LCVRPatchTarget.Universal)]
[HarmonyPatch]
internal static class IgnitionPatches
{
    [HarmonyPatch(typeof(VehicleController), nameof(VehicleController.Awake))]
    [HarmonyPostfix]
    private static void OnCarCreated(VehicleController __instance)
    {
        if (Plugin.Config.DisableCarIgnitionInteractions.Value)
            return;

        var startIgnitionObj = __instance.transform.Find("Meshes/CarKeyContainer/StartIgnition");
        var stopIgnitionObj = __instance.transform.Find("Meshes/CarKeyContainer/StopIgnition");
        var startIgnitionInteractableObject = Object.Instantiate(AssetManager.Interactable, startIgnitionObj);
        var stopIgnitionInteractableObject = Object.Instantiate(AssetManager.Interactable, stopIgnitionObj);
        
        startIgnitionInteractableObject.transform.localScale = Vector3.one * 0.5f;
        stopIgnitionInteractableObject.transform.localScale = Vector3.one * 0.5f;

        var startIgnition = startIgnitionInteractableObject.AddComponent<StartIgnition>();
        var stopIgnition = stopIgnitionInteractableObject.AddComponent<StopIgnition>();

        startIgnition.vehicle = __instance;
        stopIgnition.vehicle = __instance;
        stopIgnition.startIgnition = startIgnition;
    }
}