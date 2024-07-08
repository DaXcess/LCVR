using System.Collections.Generic;

namespace LCVR.Physics.Interactions.Car;

public class CarManager
{
    private readonly Dictionary<VehicleController, SteeringWheel> wheelCache = [];
    
    public SteeringWheel FindWheelForVehicle(VehicleController vehicle)
    {
        if (wheelCache.TryGetValue(vehicle, out var wheel))
            return wheel;

        wheel = vehicle.GetComponentInChildren<SteeringWheel>();

        if (!wheel)
            return null;

        wheelCache[vehicle] = wheel;
        return wheel;
    }

    public void OnCarDestroyed(VehicleController vehicle)
    {
        wheelCache.Remove(vehicle);
    }
}