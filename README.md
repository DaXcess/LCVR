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

Due to the amount of changes LCVR makes to the game and gameplay features, some mods might not be compatible with LCVR. For a list of compatible mods, you can refer to the compatibility sheet [here](https://docs.google.com/spreadsheets/d/1mSulrvMkQFtjF_BWDeSfGz9rm3UWKMywmUP1yhcgCGo/edit?usp=sharing). A small handful of mods have been made fully compatible with LCVR, either by custom code inside LCVR, or (preferably) changes within those mods to work properly with the VR mod.

# Installing and using the mod

It is recommended to use a mod launcher like r2modman to easily download and install the mod. You can download r2modman [here](https://thunderstore.io/package/ebkr/r2modman/). This mod can be found on thunderstore under the name [LethalCompanyVR](https://thunderstore.io/c/lethal-company/p/DaXcess/LethalCompanyVR). You can also install the mod by manually downloading it in combination with BepInEx.

Running the mod using r2modman can be done simply by clicking "Launch Modded", which will automagically launch the game with the installed mods.

For more documentation on using the mod, check out the [LethalCompanyVR Thunderstore page](https://thunderstore.io/c/lethal-company/p/DaXcess/LethalCompanyVR)

# For developers

If you want to make your mod compatible with LCVR, make sure to check out the [API documentation](Docs/API). While at the time of writing it doesn't contain much, this might be expanded more in the future.

Also make sure you know how to use BepInEx Dependencies and assembly referencing properly to make sure that your mod keeps working even when LCVR is not installed _(unless your mod **requires** LCVR to work)_.

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
