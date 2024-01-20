# Lethal Company VR Mod

[![Adding VR to Lethal Company](https://github.com/DaXcess/LCVR/blob/main/.github/assets/thumbnail.webp?raw=true)](https://www.youtube.com/watch?v=DPH_Zqpkdvc "Adding VR to Lethal Company")

<!-- Shields idea shamelessly stolen from Evaisa's LethalLib -->
[![Thunderstore Version](https://img.shields.io/thunderstore/v/DaXcess/LethalCompanyVR?style=for-the-badge&logo=thunderstore&logoColor=white)](https://thunderstore.io/c/lethal-company/p/DaXcess/LethalCompanyVR)
[![Thunderstore Downloads](https://img.shields.io/thunderstore/dt/DaXcess/LethalCompanyVR?style=for-the-badge&logo=thunderstore&logoColor=white)](https://thunderstore.io/c/lethal-company/p/DaXcess/LethalCompanyVR)

> This is the first mod that I have ever built, and also the first time using Unity so this mod might not be perfect.

<details>
  <summary>Jumpscare</summary>

  <img src="https://github.com/DaXcess/LCVR/blob/main/.github/assets/pino.jpg?raw=true" />
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

# Compatibility

Adding VR into a game will drastically change some of the gameplay elements. This is guaranteed to cause some incompatibilities with other Lethal Company mods. You can find a [compatibility sheet here](https://docs.google.com/spreadsheets/d/1mSulrvMkQFtjF_BWDeSfGz9rm3UWKMywmUP1yhcgCGo/edit?usp=sharing) where you can find a list of mods that have been tested to see if they work well in conjunction with the VR mod.

In some rare cases, a mod will have "first class support", meaning that LCVR, the mod in question, or both mods have added official compatibility for both mods to work together seamlessly. At the time of writing, the only mod that currently has first class support is [MoreCompany](https://github.com/notnotnotswipez/MoreCompany).

# Configuring the mod

Before starting the game, it is recommended to check the configuration options to see if anything needs changing. Some of the settings will change how you interact with the game in VR. Another important tab to check is **performance**. Since this game is a Unity HDRP game the performance is less optimal than it should be.

If you are not using a mod manager, then you can find the configuration inside `BepInEx/config/io.daxcess.lcvr.cfg`.

# Basic Controls

> For a list of all controls, check out the `controls` wiki page

Once you are in game, you can move around by using the left joystick. You can use the right joystick (left/right) for snap/smooth turning (if enabled) and switching inventory slot (up/down).

To sprint, press the left joystick button.

To crouch, press the right joystick button.

# Main Menu

<img src="https://github.com/DaXcess/LCVR/blob/main/.github/assets/main-menu.webp?raw=true" height="300" />

The main menu is controlled by ray interactors. You can use any controller to point towards any UI element and click using the trigger button on the corresponding controller. The only thing that has been changed on the main menu by the mod is that the keybinds settings have been disabled, since these have been hijacked by the mod.

# The Terminal

<img src="https://github.com/DaXcess/LCVR/blob/main/.github/assets/terminal.webp?raw=true" height="300" />

Since in VR you don't have access to a keyboard (under normal circumstances), the mod displays a virtual keyboard when you enter the terminal. You can use this keyboard to interact with the terminal like you would on PC.

This keyboard currently features two macros: A confirm and deny button. When pressed, these respectively send "CONFIRM" and "DENY" to the terminal. This makes it easier to switch moons and purchase items since you won't have to input this text every time.

You can exit the terminal by pressing the pause button or by clicking on the close button on the terminal keyboard.

# VR additions

<div>
  <img src="https://github.com/DaXcess/LCVR/blob/main/.github/assets/shovel.webp?raw=true" height="250" />
  <img src="https://github.com/DaXcess/LCVR/blob/main/.github/assets/spray.webp?raw=true" height="250" />
</div>

This mod, in addition to adding VR and motion controls, also adds a few special interactions that you can perform in VR. At the time of writing, these currently are:

- Spray Paint Shaking
  - When holding the spray paint item, you can physically shake it to shake the can in the game. You can also still use the secondary interact button to shake the can.
- Shovel/Sign Swinging
  - If you are holding a shovel or a sign, you'll notice that you are holding it in two hands. If you hold your controllers over your shoulder and bring them down with enough force, the mod will swing the shovel for you, dealing damage to players/entities in front of you.
