# Table of contents

1. [Adding LCVR as a dependency](#adding-lcvr-as-a-dependency)
2. [Checking if VR is active](#checking-if-vr-is-active)
3. [Creating a VR interactable object](#creating-a-vr-interactable-object)
4. [Registering custom doors](#registering-custom-doors)

## Adding LCVR as a dependency

To properly be able to interface with LCVR, you must first reference the LCVR assembly in your mod.

LCVR is available on [DaXcess' NuGet Registry](https://nuget.daxcess.io/packages/LCVR), and can be downloaded using the NuGet Package Manager in Visual Studio, or by using the `dotnet` CLI:

> Make sure you have added `https://nuget.daxcess.io/v3/index.json` to your NuGet sources list

```sh
# Optionally target a specific version by adding `--version x.x.x` at the end
dotnet add package LCVR
```

## Checking if VR is active

You can use the `VRSession.InVR` property to check whether LCVR is currently in VR mode or in flat mode.

> This used to be done via `LCVR.Plugin.Flags.HasFlag(Flags.VR)`, however an easier approach using `VRSession` has been added since v1.2.0.

```cs
using LCVR.Player;

if (VRSession.InVR)
{
    // LCVR is in VR mode
}
else
{
    // LCVR is in flatscreen mode
}
```

## Creating a VR interactable object

By default, most interactions within the game are controlled by an `InteractTrigger`. VR is compatible with these types of interactions, however since v1.2.0 some of these built in triggers have been disabled in favor of using physical interactions (e.g. having to physically press a button instead of using a "point and click" type interaction).

To start off, make sure that the object you want to create an interaction with has a collider, and that the object is set to layer 11. If you don't want this collider to actually have physics with the world, set it to trigger only. Next up, create a MonoBehaviour that will receive the VR interaction events.

Example:

```cs
public class MyVRInteractable : MonoBehaviour, VRInteractable
{
    public InteractableFlags Flags => InteractableFlags.BothHands;

    void Awake()
    {
        Debug.Log("MyVRInteractable::Awake");
    }

    void Start()
    {
        Debug.Log("MyVRInteractable::Start");
    }

    public void OnColliderEnter(VRInteractor interactor)
    {
        Debug.Log("MyVRInteractable::OnColliderEnter");
    }

    public void OnColliderExit(VRInteractor interactor)
    {
        Debug.Log("MyVRInteractable::OnColliderExit");
    }

    public bool OnButtonPress(VRInteractor interactor)
    {
        Debug.Log("MyVRInteractable::OnButtonPress");
        return true;
    }

    public void OnButtonRelease(VRInteractor interactor)
    {
        Debug.Log("MyVRInteractable::OnButtonRelease");
    }
}
```

As you can see, any VR interactable object must have a script attached which inherits from the `VRInteractable` interface. This interface requires you to implement 4 functions, and 1 property.

- `Flags`

  This property defines some flags which determine how and when this interaction should fire. Currently the only supported flags are `RightHand`, `LeftHand` and `BothHands`, which determine which hand is allowed to use this interaction.

- `OnColliderEnter`

  This method gets executed when an interactor starts intersecting with the collider. This event will only fire for one hand, so that means if the right hand starts interacting, the left hand can no longer collide or press buttons on this interaction.

- `OnColliderExit`

  This method gets executed when an interactor that has previously collided with this object leaves the collider.

- `OnButtonPress`

  This method gets executed when the `Interact` or `InteractLeft` action is performed, while the corresponding hand is inside the collision zone. The return value of this method determines whether this button press has been acknowledged or not, which determines the behaviour of this interaction going forward.

  `true` => This button press was acknowledged. `OnButtonPress` will not be called until the button has been released and pressed again. Furthermore, while the interact button is held down, the `OnColliderExit` function will not be called until the button has been released.

  `false` => This button press was **not** acknowledged. If the hand is still interacting on the next frame, and also still inside the collision zone, this function will be called again. This will repeat every frame until the button press gets acknowledged, the interact button gets released or the hand leaves the collision zone.

- `OnButtonRelease`
  This function gets called after you release the interaction button on the hand that is currently interacting with this object.

> For more in-depth examples, check out the [**built in interactions**](Physics/Interactions).

### Disabling the flatscreen interaction

While creating the interactor should be enough for VR to properly interact with it, there will still be an issue: the original `InteractTrigger`. Only adding your `VRInteractable` to your object is not enough to disable the original interaction.

To disable the original interact trigger, you must register the name of the GameObject that the interact trigger is added to to the `VRController`.

> Disabling an `InteractTrigger` like this does not affect users who are not in VR, so you don't have to check whether or not a user is in VR.

> Make sure that the object that the `InteractTrigger` is attached to has a unique name, as to not accidentally disable interactions on other objects that should not be disabled!

```cs
// Disable interact trigger
VRController.DisableInteractTrigger("NameOfGameObject");

// Re-enable interact trigger
VRController.EnableInteractTrigger("NameOfGameObject");
```

## Registering custom doors

If your mod makes use of custom doors, but still use the vanilla `DoorLock` component, some additional registering needs to be done to make them work properly for VR interactions.

You can register your custom door with LCVR by calling the `Door.RegisterDoor` method. You will need to provide the network object of your door, and optionally a modified position, rotation, and scale for the VR interactable, so you can make sure the interactable is only on the door handle, and not the entire door.

```cs
var doorHandlePosition = new Vector3(...);
var doorHandleRotation = new Vector3(...);
var doorHandleScale = new Vector3(...);

Door.RegisterDoor(myCustomDoorNetworkObject, doorHandlePosition, doorHandleRotation, doorHandleScale);
```
