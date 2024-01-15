# Lethal Company VR Mod

[<img src="https://github.com/DaXcess/LCVR/blob/main/.github/assets/thunderstore-btn.png" height="80" />](https://thunderstore.io/c/lethal-company/p/DaXcess/LethalCompanyVR)
[<img src="https://github.com/DaXcess/LCVR/blob/main/.github/assets/github-btn.png" height="80" />](https://github.com/DaXcess/LCVR/releases/latest)
<br/>

LCVR is a [BepInEx](https://docs.bepinex.dev/) mod that adds full 6DOF VR support into Lethal Company, including hand movement and motion-based controls.

The mod is powered by Unity's OpenXR plugin and is thereby compatible with a wide range of headsets, controllers and runtimes, like Oculus, Virtual Desktop, SteamVR and many more!

LCVR is compatible with multiplayer and works seamlessly with VR players and Non-VR players in the same lobby. Running this mod without having a VR headset will allow you to see the arm and head movements of any VR players in the same lobby, all while still being compatible with vanilla clients (even if the host is using no mods at all).


## Modifier Support

This fork extends LCVR by introducing support for modifiers in the bindings. Modifiers allow for more complex and customizable actions based on different conditions.
To use this feature you will have to edit the `io.daxcess.lcvr.cfg` config file with these following parameters under the `Advanced Input` category:
| Setting name  | Type          |  Default value | Description                                                                              |
| ------------- | ------------- | -------------- | ---------------------------------------------------------------------------------------- |
| EnableModifierBindings | Boolean | false       | Enable or disable modifiers support in the input                                         |
| LeftModifierDisableActions | String |          | A comma separated list of actions to disable when the left modifier is pressed. Requires modifier bindings to be enabled. |
| LeftModifierEnableActions | String |          | A comma separated list of actions to enable when the left modifier is pressed. Requires modifier bindings to be enabled. |
| RightModifierDisableActions | String |          | A comma separated list of actions to disable when the right modifier is pressed. Requires modifier bindings to be enabled. |
| RightModifierEnableActions | String |          | A comma separated list of actions to enable when the right modifier is pressed. Requires modifier bindings to be enabled. |

When using quest2 profile from [this repositery]([link-to-other-repo](https://github.com/EliasVilld/LCVR-Controller-Profiles/tree/main)). Use the providen settings.

### External Binding Profile

For an enhanced experience, we recommend using this fork in conjunction with the binding profile available `quest2` in [this repositery]([link-to-other-repo](https://github.com/EliasVilld/LCVR-Controller-Profiles/tree/main)). The external binding profile provides additional configurations and presets designed to work seamlessly with LCVR and its new modifier support.

