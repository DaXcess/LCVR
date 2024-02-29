# Default Controls

This VR mod divides the controls into two categories: **Lethal Company Controls** and **VR Controls**. The reason behind this is how they are implemented. **LC Controls** are replacing the built in controls of the game, by overriding the player input controller with them. The **VR Controls** however are exclusively used within the mod, and are not injected into the game's built in controls.

## Default LC Controls

|      Action      | Binding           | Notes                                                             |
| :--------------: | ----------------- | ----------------------------------------------------------------- |
|       Look       | HMD               | Will use right joystick X for snap-turning                        |
|       Move       | Left Joystick     |                                                                   |
|       Jump       | A                 |                                                                   |
|      Sprint      | (Disabled)        | Taken over by the mod, check `VR Inputs` below                    |
|     OpenMenu     | X                 |                                                                   |
|     Interact     | (Disabled)        | Taken over by the mod, check `VR Inputs` below                    |
|      Crouch      | R Joystick Button |                                                                   |
|       Use        | Right trigger     |                                                                   |
|   ActivateItem   | Right trigger     |                                                                   |
|     Discard      | B                 |                                                                   |
|    SwitchItem    | Right joystick Y  |                                                                   |
| ItemSecondaryUse | L Grip Button     |                                                                   |
| ItemTertiaryUse  | R Grip Button     |                                                                   |
|     PingScan     | Left Trigger      |                                                                   |
|    BuildMode     | L Grip + R Grip   | Press both buttons simultaneously to go into build mode           |
|      Delete      | B                 |                                                                   |
|  QEItemInteract  | (Disabled)        | Depricated since V45, Use secondary and tertiary use instead      |
|    EnableChat    | (Disabled)        | Chat is just not something we want to do in VR                    |
|    SubmitChat    | (Disabled)        | Chat is just not something we want to do in VR                    |
| ReloadBatteries  | (Disabled)        | Building mode prop rotating. In VR this uses Pivot in `VR Inputs` |
|   InspectItem    | (Disabled)        | Only for clipboard, disabled because there's more important stuff |
|   VoiceButton    | (Disabled)        | IDK arbitary push to talk is not very favorable in VR             |
|      Emote1      | (Disabled)        | Will not bother adding this into VR                               |
|      Emote2      | (Disabled)        | Will not bother adding this into VR                               |
| ConfirmBuildMode | (Disabled)        | Unused in the base game                                           |
|  SetFreeCamera   | (Disabled)        | Most likely a developer only cheat                                |
|    SpeedCheat    | (Disabled)        | Most likely a developer only cheat                                |

## Default VR Controls

|    Action     | Binding           | Notes                                                                                   |
| :-----------: | ----------------- | --------------------------------------------------------------------------------------- |
| Reset Height  | Y                 | Recalculates the offset between your headset and the floor                              |
|   Interact    | R Grip Button     | The grab and interact button for world interactables                                    |
| Interact Left | L Grip Button     | The grab and interact button for world interactables (left hand)                        |
|     Turn      | R Joystick X Axis | If you have snap/smooth turning enabled, this will determine the direction to rotate in |
|     Pivot     | R Joystick        | Spectator camera pivoting and build mode prop rotating                                  |
|    Sprint     | L Joystick Button | Must either be held down or toggles based on the configuration that is used             |

# How to change controls

Check out the [LCVR Controller Profiles](https://github.com/DaXcess/LCVR-Controller-Profiles) GitHub page to find a list of available controller bindings.

In the mod's configuration, set the `ControllerBindingsOverrideProfile` option to the name of the profile binding you would like to use. This does however require an active internet connection, since these profiles are downloaded directly from this GitHub repository and will allow the use of new profiles without having to update the mod.
