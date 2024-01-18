# Controls

This VR mod divides the controls into two categories: **Lethal Company Controls** and **VR Controls**. The reason behind this is how they are implemented. **LC Controls** are replacing the built in controls of the game, by overriding the player input controller with them. The **VR Controls** however are exclusively used within the mod, and are not injected into the game's built in controls.

## Default LC Controls

|      Action      | PC               | VR                | Notes                                                               |
| :--------------: | ---------------- | ----------------- | ------------------------------------------------------------------- |
|       Look       | Mouse Movement   | HMD               | Will use right joystick X for snap-turning                          |
|       Move       | WASD, Arrow Keys | Left Joystick     |                                                                     |
|       Jump       | Space            | A                 |                                                                     |
|      Sprint      | Shift            | L Joystick Button | Taken over by the mod, check `VR Inputs` below                      |
|     OpenMenu     | Escape, Tab      | X                 | Does not work with SteamVR                                          |
|     Interact     | E                | R Grip Button     | Taken over by the mod, check `VR Inputs` below                      |
|      Crouch      | Ctrl             | R Joystick Button |                                                                     |
|       Use        | LMB              | Right trigger     |                                                                     |
|   ActivateItem   | LMB              | Right trigger     |                                                                     |
|     Discard      | G                | B                 |                                                                     |
|    SwitchItem    | ScrollY          | Right joystick Y  | Its expecting an axis so this should be fine                        |
| ItemSecondaryUse | Q                | L Grip Button     |                                                                     |
| ItemTertiaryUse  | E                | R Grip Button     |                                                                     |
|     PingScan     | RMB              | Left Trigger      |                                                                     |
|  QEItemInteract  | Q, E             | (Disabled)        | Depricated since V45, Use secondary and tertiary use instead        |
|    EnableChat    | Slash            | (Disabled)        | Chat is just not something we want to do in VR                      |
|    SubmitChat    | Enter            | (Disabled)        | Chat is just not something we want to do in VR                      |
| ReloadBatteries  | R                | (Disabled)        | I have never seen batteries in this game, maybe in a future update? |
|   InspectItem    | Z                | (Disabled)        | Only for clipboard, disabled because there's more important stuff   |
|   VoiceButton    | T                | (Disabled)        | IDK arbitary push to talk is not very favorable in VR               |
|      Emote1      | 1                | (Disabled)        | Will not bother adding this into VR                                 |
|      Emote2      | 2                | (Disabled)        | Will not bother adding this into VR                                 |
|    BuildMode     | B                | (Disabled)        | If we don't have enough buttons on VR controllers, disable this     |
| ConfirmBuildMode | V                | (Disabled)        | If we don't have enough buttons on VR controllers, disable this     |
|      Delete      | X                | (Disabled)        | If we don't have enough buttons on VR controllers, disable this     |
|  SetFreeCamera   | C                | (Disabled)        | Most likely a developer only cheat                                  |
|    SpeedCheat    | H                | (Disabled)        | Most likely a developer only cheat                                  |

## Default VR Controls

|    Action    | Bind              | Notes                                                                                   |
| :----------: | ----------------- | --------------------------------------------------------------------------------------- |
| Reset Height | Y                 | Recalculates the offset between your headset and the floor                              |
|     Grab     | R Grip Button     | The grab and interact button for world interactables                                    |
|     Turn     | R Joystick X Axis | If you have snap/smooth turning enabled, this will determine the direction to rotate in |
|    Pivot     | R Joystick        | Spectator camera pivoting                                                               |
|    Sprint    | L Joystick Button | Must either be held down or toggles based on the configuration that is used             |

# How to change controls

Check out the [LCVR Controller Profiles](https://github.com/DaXcess/LCVR-Controller-Profiles) GitHub page to find a list of available controller bindings.

In the mod's configuration, set the `ControllerBindingsOverrideProfile` option to the name of the profile binding you would like to use. This does however require an active internet connection, since these profiles are downloaded directly from this GitHub repository and will allow the use of new profiles without having to update the mod.
