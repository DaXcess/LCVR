# Lethal Company VR Mod

<!-- Shields idea shamelessly stolen from Evaisa's LethalLib -->

[![Thunderstore Version](https://img.shields.io/thunderstore/v/DaXcess/LethalCompanyVR?style=for-the-badge&logo=thunderstore&logoColor=white)](https://thunderstore.io/c/lethal-company/p/DaXcess/LethalCompanyVR)
[![GitHub Version](https://img.shields.io/github/v/release/DaXcess/LCVR?style=for-the-badge&logo=github)](https://github.com/DaXcess/LCVR/releases/latest)
[![Thunderstore Downloads](https://img.shields.io/thunderstore/dt/DaXcess/LethalCompanyVR?style=for-the-badge&logo=thunderstore&logoColor=white)](https://thunderstore.io/c/lethal-company/p/DaXcess/LethalCompanyVR)
[![GitHub Downloads](https://img.shields.io/github/downloads/DaXcess/LCVR/total?style=for-the-badge&logo=github)](https://github.com/DaXcess/LCVR/releases/latest)
<br />
[![Release Build](https://img.shields.io/github/actions/workflow/status/DaXcess/LCVR/build-release.yaml?branch=main&style=for-the-badge&label=RELEASE)](https://github.com/DaXcess/LCVR/actions/workflows/build-release.yaml)
[![Debug Build](https://img.shields.io/github/actions/workflow/status/DaXcess/LCVR/build-debug.yaml?branch=dev&style=for-the-badge&label=DEBUG)](https://github.com/DaXcess/LCVR/actions/workflows/build-debug.yaml)

LCVR is a [BepInEx](https://docs.bepinex.dev/) mod that adds full 6DOF VR support into Lethal Company, including hand movement and motion-based controls.

The mod is powered by Unity's OpenXR plugin and is thereby compatible with a wide range of headsets, controllers and runtimes, like Oculus, Virtual Desktop, SteamVR and many more!

LCVR is compatible with multiplayer and works seamlessly with VR players and non-VR players in the same lobby. Running this mod without having a VR headset will allow you to see the arm and head movements of any VR players in the same lobby, all while still being compatible with vanilla clients (even if the host is using no mods at all).

### Discord Server

Facing issues, have some mod (in)compatibility to report or just want to hang out?

You can join the [LCVR Discord Server](https://discord.gg/2DxNgpPZUF)!

# Compatibility

Due to the amount of changes LCVR makes to the game and gameplay features, some mods might not be compatible with LCVR. A small handful of mods have been made fully compatible with LCVR, either by custom code inside LCVR, or (preferably) changes within those mods to work properly with the VR mod.

# Installing and using the mod

It is recommended to use a mod launcher like Gale to easily download and install the mod. You can download Gale [here](https://thunderstore.io/package/Kesomannen/GaleModManager/). This mod can be found on thunderstore under the name [LethalCompanyVR](https://thunderstore.io/c/lethal-company/p/DaXcess/LethalCompanyVR). You can also install the mod by manually downloading it in combination with BepInEx.

Running the mod using Gale can be done simply by clicking "Launch game", which will automagically launch the game with the installed mods.

For more documentation on using the mod, check out the [LethalCompanyVR Thunderstore page](https://thunderstore.io/c/lethal-company/p/DaXcess/LethalCompanyVR)

# Versions

Here is a list of LCVR versions and which version(s) of Lethal Company it supports

| LCVR              | Lethal Company    | [Configuration version](Docs/Configuration/README.md) |
|-------------------|-------------------|-------------------------------------------------------|
| v1.4.3 *(LATEST)* | V73               | 1                                                     |
| v1.4.2            | V73               | 1                                                     |
| v1.4.1            | V73               | 1                                                     |
| v1.4.0            | V72               | 1                                                     |
| v1.3.10           | V64 - V69.1       | *N/A*                                                 |
| v1.3.9            | V64 - V69.1       | *N/A*                                                 |
| v1.3.8            | V64 - V69         | *N/A*                                                 |
| v1.3.7            | V64 - V67         | *N/A*                                                 |
| v1.3.6*           | V64 - V67         | *N/A*                                                 |
| v1.3.5            | V64 - V66         | *N/A*                                                 |
| v1.3.4            | V64 and V64.1     | *N/A*                                                 |
| v1.3.3            | V64 and V64.1     | *N/A*                                                 |
| v1.3.2            | V64               | *N/A*                                                 |
| v1.3.1            | V62               | *N/A*                                                 |
| v1.3.0            | V56               | *N/A*                                                 |
| v1.2.5            | V50               | *N/A*                                                 |
| v1.2.4            | V50               | *N/A*                                                 |
| v1.2.3            | V50               | *N/A*                                                 |
| v1.2.2            | V50               | *N/A*                                                 |
| v1.2.1            | V50 (Older patch) | *N/A*                                                 |
| v1.2.0            | V49               | *N/A*                                                 |
| v1.1.9            | V45 and V49       | *N/A*                                                 |
| v1.1.8            | V45 and V49       | *N/A*                                                 |
| v1.1.6            | V45 and V49       | *N/A*                                                 |
| v1.1.5            | V45 and V49       | *N/A*                                                 |
| v1.1.4            | V45 and V49       | *N/A*                                                 |
| v1.1.2            | V45 and V49       | *N/A*                                                 |
| v1.1.1            | V45 and V49       | *N/A*                                                 |
| v1.1.0            | V45 and V49       | *N/A*                                                 |
| v1.0.1            | V45 and V49       | *N/A*                                                 |
| v1.0.0            | V45 and V49       | *N/A*                                                 |

> \* LCVR versions from 1.3.6 and above also check hashes remotely, meaning newer Lethal Company versions might be supported even though they aren't listed here.

# For developers

If you want to make your mod compatible with LCVR, make sure to check out the [API documentation](Docs/API). While at the time of writing it doesn't contain much, this might be expanded more in the future.

Also make sure you know how to use BepInEx Dependencies and assembly referencing properly to make sure that your mod keeps working even when LCVR is not installed _(unless your mod **requires** LCVR to work)_.

# Install from source

> The easiest way to install the mod is by downloading it from Thunderstore. You only need to follow these steps if you are planning on installing the mod by building the source code and without a mod manager.

To install the mod from the source code, you will first have to compile the mod. Instructions for this are available in [COMPILING.md](COMPILING.md).

Next up you'll need to grab a copy of some **Runtime Dependencies** and the **Asset Bundles**. You can grab both of these from [the thunderstore branch](https://github.com/DaXcess/LCVR/tree/thunderstore). You can also manually retrieve the **Runtime Dependencies** from a manually compiled Unity project.

## Retrieving Runtime Dependencies from a Unity Project

> You can skip this part if you have taken the runtime dependencies from the releases page.

First of all start by installing Unity 2022.3.9f1, which is the Unity version that Lethal Company uses. Once you have installed the editor, create a new Unity project. If you are planning on adding prefabs to the mod, use the HDRP template and add the XR modules via the HDRP helper or by manually installing the Unity OpenXR plugins (Google is your friend), otherwise you can just use the VR template.

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

To install BepInEx, you can follow their [Installation Guide](https://docs.bepinex.dev/articles/user_guide/installation/index.html#installing-bepinex-1).

## Installing the mod

Once BepInEx has been installed and run at least once, you can start installing the mod.

First of all, in the `BepInEx/plugins` folder, create a new folder called `LCVR` (doesn't have to be named that specifically, but makes identification easier). Inside this folder, place the `LCVR.dll` file that was generated during the [COMPILING.md](COMPILING.md) steps.

After this has been completed, create a new directory called `RuntimeDeps` (has to be named exactly that) inside the `LCVR` folder. Inside this folder you will need to put the following DLLs:

- UnityEngine.SpatialTracking.dll
- Unity.XR.CoreUtils.dll
- Unity.XR.Interaction.Toolkit.dll
- Unity.XR.Management.dll
- Unity.XR.OpenXR.dll

These files should have been retrieved either during the [Retrieving Runtime Dependencies](#retrieving-runtime-dependencies-from-a-unity-project) step, or from grabbing them from the latest release.

Next up, grab the **Asset Bundles** from one of the releases, and place them into the same folder as the `LCVR.dll` file. These asset bundle files needs to be called `lethalcompanyvr` and `lethalcompanyvr-levels` (also make sure not to mix these, since the content of these files are different).

Finally, in the `BepInEx/patchers` folder, also create a new folder called `LCVR` (again, doesn't have to be exact). Inside this folder, place the `LCVR.Preload.dll` file that was also generated during the [COMPILING.md](COMPILING.md) steps.

In this folder, also create a new directory called `RuntimeDeps` (again, has to be exactly named that), and place the following DLLs inside:

- openxr_loader.dll
- UnityOpenXR.dll

You can now run the game with LCVR installed.
