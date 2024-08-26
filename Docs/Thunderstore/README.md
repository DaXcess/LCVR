# Lethal Company VR Mod

<!-- Shields idea shamelessly stolen from Evaisa's LethalLib -->

[![Thunderstore Version](https://img.shields.io/thunderstore/v/DaXcess/LethalCompanyVR?style=for-the-badge&logo=thunderstore&logoColor=white)](https://thunderstore.io/c/lethal-company/p/DaXcess/LethalCompanyVR)
[![GitHub Version](https://img.shields.io/github/v/release/DaXcess/LCVR?style=for-the-badge&logo=github)](https://github.com/DaXcess/LCVR/releases/latest)
[![Thunderstore Downloads](https://img.shields.io/thunderstore/dt/DaXcess/LethalCompanyVR?style=for-the-badge&logo=thunderstore&logoColor=white)](https://thunderstore.io/c/lethal-company/p/DaXcess/LethalCompanyVR)
[![GitHub Downloads](https://img.shields.io/github/downloads/DaXcess/LCVR/total?style=for-the-badge&logo=github)](https://github.com/DaXcess/LCVR/releases/latest)
<br/>
[![Release Build](https://img.shields.io/github/actions/workflow/status/DaXcess/LCVR/build-release.yaml?branch=main&style=for-the-badge&label=RELEASE)](https://github.com/DaXcess/LCVR/actions/workflows/build-release.yaml)
[![Debug Build](https://img.shields.io/github/actions/workflow/status/DaXcess/LCVR/build-debug.yaml?branch=dev&style=for-the-badge&label=DEBUG)](https://github.com/DaXcess/LCVR/actions/workflows/build-debug.yaml)

> This is the first mod that I have ever built, and also the first time using Unity so this mod might not be perfect.

<details>
  <summary>Jumpscare</summary>

  <img src="https://github.com/DaXcess/LCVR/blob/assets/pino.jpg?raw=true" />
</details>
<br/>

Ready to immersive yourself into the horrors of Lethal Company with Virtual Reality? Well wait no longer!

LCVR is a mod that adds full 6DOF VR support into Lethal Company, including hand movement and motion-based controls.

The mod is powered by Unity's OpenXR plugin and is thereby compatible with a wide range of headsets, controllers and runtimes, like Oculus, Virtual Desktop, SteamVR and many more!

LCVR is compatible with multiplayer and works seamlessly with VR players and Non-VR players in the same lobby. Running this mod without having a VR headset will allow you to see the arm and head movements of any VR players in the same lobby, all while still being compatible with vanilla clients (even if the host is using no mods at all).

### Open Source

The source code for this mod is available on GitHub! Check it out: [DaXcess/LCVR](https://github.com/DaXcess/LCVR).

### License

This mod is licensed under the GNU General Public License version 3 (GPL-3.0). For more info check [LICENSE](https://github.com/DaXcess/LCVR/blob/main/LICENSE).

### Verifying mod signature

> If you don't care about this, skip this part.

LCVR comes pre-packaged with a digital signature. You can use tools like GPG to verify the `LCVR.dll.sig` signature with the `LCVR.dll` plugin file.

The public key which can be used to verify the file is [9422426F6125277B82CC477DCF78CC72F0FD5EAD (OpenPGP Key Server)](https://keys.openpgp.org/vks/v1/by-fingerprint/9422426F6125277B82CC477DCF78CC72F0FD5EAD).

### Bypassing integrity checks

To prevent completely destroying the game, this mod scans the game assembly and tries to detect whether it's using a supported version or not. If this check fails, the mod will assume that either the game was updated, or the game files have been corrupted, and will refuse to start the mod. You can disable this behaviour by passing `--lcvr-skip-checksum` to the game's launch options in Steam.

### Discord Server

Facing issues, have some mod (in)compatibility to report or just want to hang out?

You can join the [LCVR Discord Server](https://discord.gg/2DxNgpPZUF)!

# Versions

> Versions annotated with **(BETA)** are not available on Thunderstore, and must be downloaded or compiled manually

Here is a list of LCVR versions and which version(s) of Lethal Company it supports

| LCVR              | Lethal Company    |
|-------------------|-------------------|
| v1.3.2 *(LATEST)* | V62               |
| v1.3.1            | V62               |
| v1.3.0            | V56               |
| v1.2.5            | V50               |
| v1.2.4            | V50               |
| v1.2.3            | V50               |
| v1.2.2            | V50               |
| v1.2.1            | V50 (Older patch) |
| v1.2.0            | V49               |
| v1.1.9            | V45 and V49       |
| v1.1.8            | V45 and V49       |
| v1.1.6            | V45 and V49       |
| v1.1.5            | V45 and V49       |
| v1.1.4            | V45 and V49       |
| v1.1.2            | V45 and V49       |
| v1.1.1            | V45 and V49       |
| v1.1.0            | V45 and V49       |
| v1.0.1            | V45 and V49       |
| v1.0.0            | V45 and V49       |

# Compatibility

Most mods should all work fine with LCVR, like interior mods, new moons, most items, etc. There's also a small handful of mods that have been explicitly made compatible with VR.

## Known incompatibilities

In general, most emote mods, mods adding UI elements and mods that require new bindings are not compatible with LCVR by default, and either require configuration changes, or dedicated VR support.

# Configuring the mod

You can change the mod configuration from within the game itself. Just launch the game with the VR mod installed, get to the main menu, and press the big VR button on the right side of the screen. This will open a big settings menu where you can configure the VR mod to your liking.

> _When creating a modpack or profile code, it is recommended to **NOT** ship your config file, so that other people can configure it on their own using the default settings. To quickly reset the settings, delete the config file named `io.daxcess.lcvr.cfg` from the `BepInEx/config` directory._

# Controls

LCVR attempts to automatically detect which type of controller you are using, and will automatically apply the correct controller profile once they have been detected.

The current list of built-in controller profiles are:

- Oculus (Rift S, Quest 2) - Default Fallback
- Meta Quest (Quest 3)
- Valve Index
- HTC Vive
- HP Reverb G2
- Windows Mixed Reality

For a list of all controls for your specific controllers, check out the `controls` wiki pages.

# How to change controls

You can change controller bindings just like you would normally in Lethal Company.

Go to the settings, then press "Change keybinds", and scroll all the way down to the VR controls section.

> You must be in VR to change VR controller bindings. <br/>
> Resetting the bindings will only reset the VR bindings, and will not touch keyboard/gamepad bindings.

# Main Menu

<img src="https://github.com/DaXcess/LCVR/blob/assets/main-menu.webp?raw=true" height="300" />

The main menu is controlled by ray interactors. You can use any controller to point towards any UI element and click using the trigger button on the corresponding controller. In the main menu you also have access to a keyboard when you focus any input element, so that you can change your lobby name, tags, or change settings using the VR settings menu.

# The Terminal

<img src="https://github.com/DaXcess/LCVR/blob/assets/terminal.webp?raw=true" height="300" />

Since in VR you don't have access to a keyboard (under normal circumstances), the mod displays a virtual keyboard when you enter the terminal. You can use this keyboard to interact with the terminal like you would on PC.

This keyboard currently features two macros: A confirm and deny button. When pressed, these respectively send "CONFIRM" and "DENY" to the terminal. This makes it easier to switch moons and purchase items since you won't have to input this text every time.

You can exit the terminal by pressing the pause button or by clicking on the close button on the terminal keyboard.

# VR Interactions

LCVR features a bunch of new interactions that VR players can use to interact with the world around them, without having to use a boring invisible laser and a simple controller binding.

> All of the following interactions can be disabled individually inside the config

- **Ship Lever**

  You now must physically pull/push the ship lever to land the ship or take off from a planet. The lever, when held, will follow the position of your hand, and this even works for other players who have the mod!

- **Monitor Buttons**

  You may notice that the monitor buttons have been moved next to the lever. This is because you can now physically press the buttons to turn on/off the monitor, or switch to another player on the radar!

- **Charging Station**

  Hate being forced to stand in front of the charging station every time you charge an item? Well now you can just hold any item that has a battery, and just hold it up to the charging station. Voila, your item has now been charged. If you pull the item out too quickly though, the charger will not charge your item!
  _This interaction only works on the right hand. Putting your left hand inside the charger will make you just look like an idiot._

- **Ship Door**

  Have an angry dog chasing you around? Just smash the ship door buttons to close or open the ship door.

- **Teleporter**

  Want to inverse into the facility with style? Just flick open the glass cover, and **SMASH** the teleporter button with your fist!

- **Company Bell**

  Delicately place your finger on top of the bell to make it ring... Or just smash it, you do you.

- **Ship Horn**

  Pull the ship horn cord using your hand. Yup.

- **Breaker Box**

  Y'all ever had issues with trying to flip the switches on the breaker box in VR? It's so stupid because their hitboxes are gigantic!
  Anyways, just flick open the door with your hand, and use your finger to toggle the switches.
  _This interaction only works when you are using your pointer finger, a fist or flat hand will not work_

- **Doors**
  Always had the issue where like a billion people tried to open the same door and it just keeps opening and closing and you can't get through? Well now you actually have to interact with the door handle to open and close the door. Is a door locked? Find out by trying to open the door and listen for the sound cue (or just notice that it doesn't open, whatever). To use a key on a door, interact with the door handle using your right hand while holding a key. Same thing for the lockpicker, however picking up the lockpicker when it is placed on a door also requires you to physically grab it. When the lockpicker is an item on the floor, it will behave normally, and can be picked up from a distance.

- **Face**

  Just want to really scream right into that walkie, begging to be teleported because a Jester is right around the corner? Well, you can now do so without pressing any button! Just hold up any compatible item to your face to use them, but watch out what you all put near your face!
  _This interaction only works on the right hand, for obvious reasons_

### Muffle

Hate it when you die to a dog because your frantic screaming caused you to lure the canines towards your location? Just hold your hand in front of your mouth, and none of the enemies will be able to hear you anymore! As a bonus, anyone with the VR mod will now hear your voice muffled, as if you got snatched by a snare flea. However be warned, the longer you hold your hand in front of your mouth, the less you will be able to see _(only until a hard limit, you will not be completely blinded)_.

# VR additions

<div>
  <img src="https://github.com/DaXcess/LCVR/blob/assets/shovel.webp?raw=true" height="250" />
  <img src="https://github.com/DaXcess/LCVR/blob/assets/spray.webp?raw=true" height="250" />
</div>

This mod, in addition to adding VR and motion controls, also adds a few special interactions that you can perform in VR. At the time of writing, these currently are:

- Spray Paint Shaking
  - When holding the spray paint item, you can physically shake it to shake the can in the game. You can also still use the secondary interact button to shake the can.
- Shovel/Sign Swinging
  - If you are holding a shovel or a sign, you'll notice that you are holding it in two hands. If you hold your controllers over your shoulder and bring them down with enough force, the mod will swing the shovel for you, dealing damage to players/entities in front of you.

# Free Roam Spectating

> Free Roam Spectator provided by The Company™ Device©. _"Experience death like nobody has ever before! It's amazing!"_

Hate having to just watch a flat screen where your fellow employees die to the horrors of the facilities? Well fear no more! With the new Company™ Device© you retain the rights to wander the desolate planets even when your physical body is no longer showing signs compatible with life!

_Since the company was a big fan of using Linux for the Device©, the colors look more gray when dead since they cheaped out on the HDR support._

You can teleport to other employees, like you would using the old spectator view, by using the **Interact** _(Default: Right Controller Trigger)_ button. This will cycle through each employee in the lobby that has not yet met their maker. Use this to quickly see how a fellow employee is going about their day, or to get unstuck if you have fallen into a pit.

_Since the Device© is making use of simulated consciousness, physical barriers like doors act like air, so you can walk right through them no problem!_

Afraid of the dark? Use the **Drop Item** _(Default: B)_ button to toggle night vision! When enabled, this light will illuminate the world and facilities around you, so that you can see what your still breathing fellow employees can't!

_Another issue of the simulated consciousness is that you can no longer interact with the world around you. You are only able to use ladders and entrance doors, like fire exits and the main entrance. The Company™ has explained in a statement that they are not planning on fixing this issue._

Want to hide that pesky "you are dead lol" interface? Just press the **Secondary Use** _(Default: Left Controller Grip)_ button to toggle the interface.
