# Lethal Company VR Mod

<!-- Shields idea shamelessly stolen from Evaisa's LethalLib -->
[![Thunderstore Version](https://img.shields.io/thunderstore/v/DaXcess/LethalCompanyVR?style=for-the-badge&logo=thunderstore&logoColor=white)](https://thunderstore.io/c/lethal-company/p/DaXcess/LethalCompanyVR)
[![Thunderstore Downloads](https://img.shields.io/thunderstore/dt/DaXcess/LethalCompanyVR?style=for-the-badge&logo=thunderstore&logoColor=white)](https://thunderstore.io/c/lethal-company/p/DaXcess/LethalCompanyVR)

[<img src="https://github.com/DaXcess/LCVR/blob/main/.github/assets/thunderstore-btn.png" height="80" />](https://thunderstore.io/c/lethal-company/p/DaXcess/LethalCompanyVR)
[<img src="https://github.com/DaXcess/LCVR/blob/main/.github/assets/github-btn.png" height="80" />](https://github.com/DaXcess/LCVR/releases/latest)
<br/>

LCVR is a [BepInEx](https://docs.bepinex.dev/) mod that adds full 6DOF VR support into Lethal Company, including hand movement and motion-based controls.

The mod is powered by Unity's OpenXR plugin and is thereby compatible with a wide range of headsets, controllers and runtimes, like Oculus, Virtual Desktop, SteamVR and many more!

LCVR is compatible with multiplayer and works seamlessly with VR players and non-VR players in the same lobby. Running this mod without having a VR headset will allow you to see the arm and head movements of any VR players in the same lobby, all while still being compatible with vanilla clients (even if the host is using no mods at all).

### Discord Server

Facing issues, have some mod (in)compatibility to report or just want to hang out?

You can join the [LCVR Discord Server](https://discord.gg/2DxNgpPZUF)!

# Compatibility

LCVR should be fully compatible with [MoreCompany](https://github.com/notnotnotswipez/MoreCompany) and has first class compatibility support from this mod. At the time of writing, there are no other mods that have first class compatibility support, however lots of them are compatible out of the box. You can find a compatibility sheet [here](https://docs.google.com/spreadsheets/d/1mSulrvMkQFtjF_BWDeSfGz9rm3UWKMywmUP1yhcgCGo/edit?usp=sharing).

# Using the mod

Once you have installed LCVR via your preferred method, you can now start the game. If it is your first time running the game with this mod, you will notice a warning on the first main menu when the game starts up, which is telling you to restart the game if you want to play in VR. If you are using the mod without VR, you can ignore this warning and continue. If you are using the mod **with** VR, restart your game, and VR will be initialized correctly.

## Configuring the mod

Before starting the game, you can make changes to the configuration file to streamline your VR experience. If you are using r2modman or Thunderstore Mod Manager <sub><sup>(then please switch to r2modman)</sup></sub>, then you can use their built in configuration editor to make the changes you desire.

If you are not using a mod manager, then you can find the configuration inside `BepInEx/config/io.daxcess.lcvr.cfg`.

## Navigating the main menu

The main menu is controlled by ray interactors. You can use any controller to point towards any UI element and click using the trigger button on the corresponding controller. The only thing that has been changed on the main menu by the mod is that the keybinds settings have been disabled, since these have been hijacked by the mod.

## Basic controls

Once you are in game, you can move around by using the left joystick. You can use the right joystick (left/right) for snap/smooth turning (if enabled) and switching inventory slot (up/down).

To sprint, press the left joystick button.

To crouch, press the right joystick button.

For more keybinds, check out [KEYBINDS.md](KEYBINDS.md).

## The Terminal

Since in VR you don't have access to a keyboard (under normal circumstances), the mod displays a virtual keyboard when you enter the terminal. You can use this keyboard to interact with the terminal like you would on PC.

This keyboard currently features two macros: A confirm and deny button. When pressed, these respectively send "CONFIRM" and "DENY" to the terminal. This makes it easier to switch moons and purchase items since you won't have to input this text every time.

You can exit the terminal by pressing the pause button or by clicking on the close button on the terminal keyboard.

## Spectating

When you die, you'll be sent to a black void with a screen in front of you (not unlike the main menu), which is where you will live out the rest of the day. You can pivot the spectator camera by using the right controller's joystick.

To spectate the next player, you can utilise the right trigger button. To vote to leave early, use the left trigger button.

## VR additions

This mod, in addition to adding VR and motion controls, also adds a few special interactions that you can perform in VR. At the time of writing, these currently are: 
* Spray Paint Shaking
  * When holding the spray paint item, you can physically shake it to shake the can in the game. You can also still use the secondary interact button to shake the can.
* Shovel/Sign Swinging
  * If you are holding a shovel or a sign, you'll notice that you are holding it in two hands. If you hold your controllers over your shoulder and bring them down with enough force, the mod will swing the shovel for you, dealing damage to players/entities in front of you.

# Install from source

> The easiest way to install the mod is by downloading it from Thunderstore. You only need to follow these steps if you are planning on installing the mod by building the source code and without a mod manager.

To install the mod from the source code, you will first have to compile the mod. Instructions for this are available in [COMPILING.md](COMPILING.md).

Next up you'll need to grab a copy of some **Runtime Dependencies**. You can either grab these from [one of the releases](https://github.com/DaXcess/LCVR/releases), or if you truly want the no hand holding experience, you can retrieve them from a Unity project.

## Retrieving Runtime Dependencies from a Unity Project

> You can skip this part if you have taken the runtime dependencies from the releases page.

First of all start by installing Unity 2022.3.9f1, which is the Unity version that Lethal Company uses. Once you have installed the editor, create a new Unity project. If you are planning on adding prefabs to the mod, use the HDRP template and add the XR modules via the HDRP helper or by manually installing the Unity OpenXR plugins (Google is your friend). Otherwise you can just use the VR template.

Make sure you set the scripting backend to Mono, and not to Il2Cpp (Unity will warn you when you try to compile a VR game with Il2Cpp enabled). You can now build your dummy game. Once the game is built you can navigate to it's `<Project Name>_Data/Managed` directory. There you will need to extract the following files:

- UnityEngine.SpatialTracking.dll
- Unity.XR.CoreUtils.dll
- Unity.XR.Interaction.Toolkit.dll
- Unity.XR.Management.dll
- Unity.XR.OpenXR.dll

And from the `<Project Name>_Data/Plugins/x86_64` directory:

- openxr_loader.dll
- UnityOpenXR.dll

## Install BepInEx

BepInEx is the modloader that LCVR uses to mod the game. You can download BepInEx from their [GitHub Releases](https://github.com/BepInEx/BepInEx/releases) (LCVR currently targets BepInEx 5.4.22).

To install BepInEx, you can follow their [Installation Gude](https://docs.bepinex.dev/articles/user_guide/installation/index.html#installing-bepinex-1).

## Installing the mod

Once BepInEx has been installed and run at least once, you can start installing the mod.

First of all, in the `BepInEx/plugins` folder, create a new folder called `LCVR` (doesn't have to be named that specifically, but makes identification easier). Inside this folder, place the `LCVR.dll` file that was generated during the [COMPILING.md](COMPILING.md) steps.

After this has been completed, create a new directory called `RuntimeDeps` (has to be named exactly that) inside of the `LCVR` folder. Inside this folder you will need to put the DLLs that you have retrieved during the [Retrieving Runtime Depenencies](#retrieving-runtime-dependencies-from-a-unity-project) step. You can now run the game with LCVR installed.
