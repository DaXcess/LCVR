# 1.2.1

**Bug fixes:**

- Fixed corrupt/improper OpenXR setup causing the settings menu to not load
- Fixed a benign warning when loading input bindings
- Reduced impact of playerspace spoofing hacks
- Fixed an issue where somehow getting the camera out of water would prevent drowning
- Fixed Diversity custom pass warping rendering when DynRes is enabled

# 1.2.0

## Settings Menu

In the main menu screen, you will notice a new button being present: **VR settings**.
This button is visible on both flat screen and in VR, and allows you to change the configuration of the mod without having to use your mod manager, or manually having to edit the configuration using a text editor.

_Most of these settings were already configurable since 1.0.0, it has only been made easier to change them in this update._

You're also able to swap your OpenXR runtime using this settings menu, instead of annoyingly having to change your default OpenXR runtime within their dedicated apps.

## Interactions

LCVR v1.2.0 features a bunch of new interactions that VR players can use to interact with the world around them!

> All of the following interactions can be disabled individually inside the config.

- **Ship Lever**

  You now must physically pull/push the ship lever to land the ship or take off from a planet. The lever, when held, will follow the position of your hand, and this even works for other players who have the mod!

- **Monitor Buttons**

  You may notice that the monitor buttons have been moved next to the lever. This is because you can now physically press the buttons to turn on/off the monitor, or switch to another player on the radar!

- **Charging Station**

  Hate being forced to stand in front of the charging station every time you charge an item? Well now you just hold any item that has a battery, and just hold it up to the charging station. Voila, your item has now been charged. If you pull the item out too quickly though, the charger will not charge your item!
  _This interaction only works on the right hand, putting your left hand inside the charger will make you just look like an idiot._

- **Ship Door**

  Have an angry dog chasing you around? Just smash the ship door buttons to close or open the ship door.

- **Teleporter**

  Want to inverse into the facility with style? Just flick open the glass cover, and **SMASH** the teleporter button with your fist!

- **Company Bell**

  Delicately place your finger on top of the bell to make it ring... Or just smash it, you do you.

- **Ship Horn**

  Funny little horn with a funny little cord can now be pulled using your funny little hand.

- **Breaker Box**

  Y'all ever had issues with trying to flip the switches on the breaker box in VR? It's so stupid because their hitboxes are gigantic!
  Anyways, just flick open the door with your hand, and use your finger to toggle the switches.
  _This interaction only works when you are using your pointer finger, a fist or flat hand will not work_

- **Doors**

  Always had the issue where like a billion people tried to open the same door and it just keeps opening and closing and you can't get through? Well now you actually have to interact with the door handle to open and close the door. Is a door locked? Find out by trying to open the door and listen for the sound cue (or just notice that it doesn't open, whatever). To use a key on a door, interact with the door handle using your right hand while holding a key. Same thing for the lockpicker, however picking up the lockpicker when it is placed on a door also requires you to physically grab it. When the lockpicker is an item on the floor, it will behave normally, and can be picked up from a distance.

- **Face**

  Just want to really scream right into that walkie, begging to be teleported because a Jester is right around the corner? Well, you can now do so without pressing any button! Just hold up any compatible item to your face to use them, but watch out what you all put near your face!
  _This interaction only works on the right hand, for obvious reasons_

## Muffle

Hate it when you die to a dog because your frantic screaming caused you to lure the canines towards your location? Just hold your hand in front of your mouth, and none of the enemies will be able to hear you anymore! As a bonus, anyone with the VR mod will now hear your voice muffled, as if you got snatched by a snare flea. However be warned, the longer you hold your hand in front of your mouth, the less you will be able to see!

## Fixed broken arms

Replaced the games IK constraints with ones that are properly able to determine the position of the elbow, meaning the arms should no longer bend inwards.

## Fixed controller auto-detection

In versions before 1.2.0, the automatic detection of the type of controller being used happened too early in the loading process, causing a large amount of users to default to the `default` controller profile, which is only meant for Oculus/Meta (or similar ABXY) devices. In version 1.2.0 going forward, this auto-detection keeps running in the background until a match is found, even when already in a level (though the detection should complete once you start using the controllers in the main menu, but maybe some mods bypass the menu screen entirely).

## Locomotion Update

You can now lean over fencing and smaller objects easier without getting pushed back immediately!
Keep in mind that if you for some reason poke your head inside of a wall, or start moving using your controllers, you will be pushed out of any wall you might be intersecting with!

## Free Roam Spectating

> Free Roam Spectator provided by The Company™ Device©. _"Experience death like nobody has ever before! It's amazing!"_

Hate having to just watch a flat screen where your fellow employees die to the horrors of the facilities? Well fear no more! With the new Company™ Device© you retain the rights to wander the desolate planets even when your physical body is no longer showing signs compatible with life!

_Since the company was a big fan of using Linux for the Device©, the colors look more gray when dead since they cheaped out on the HDR support._

You can teleport to other employees, like you would using the old spectator view, by using the **Interact** _(Default: Right Controller Trigger)_ button. This will cycle through each employee in the lobby that has not yet met their maker. Use this to quickly see how a fellow employee is going about their day, or to get unstuck if you have fallen into a pit.

_Since the Device© is making use of shared simulated consciousness, physical barriers like doors act like air, so you can walk right through them no problem!_

Afraid of the dark? Use the **Drop Item** _(Default: B)_ button to toggle night vision! When enabled, this light will illuminate the world and facilities around you, so that you can see what your still breathing fellow employees can't!

_Another issue of the simulated consciousness is that you can no longer interact with the world around you. You are only able to use ladders and entrance doors, like fire exits and the main entrance. The Company™ has explained in a statement that they are not planning on fixing this issue._

Want to hide that pesky "you are dead lol" interface? Just press the **Secondary Use** _(Default: Left Controller Grip)_ button to toggle the interface.

# 1.1.9

**Bug fixes:**

- Fixed helmet (and volumetric plane) positioning after death

# 1.1.8

**Bug fixes:**

- Fixed event handler leak in the Keyboard causing the game to hang
- Fixed weird fog rendering issue in the left eye when the radar is active

**Added configuration:**

- Added new configuration option `EnableHelmetVisor` (Default: false). When enabled, will show the first person visor model.

# 1.1.7

**Adjustments:**

- Improved keyboard behavior. It now also properly works in the pause menu.
- When sprint toggle is enabled, sprint will be disabled when in a special interaction animation

**Bug fixes:**

- Fixed ray interactors not working when leaving a game

# 1.1.6

**Adjustments:**

- Changed some of the locomotion code
- Removed version checking on compatible mods
- Made the Nutcracker less sensitive to head rotations
- _**Don't** turn around..._

**Bug fixes:**

- Fixed menu button not closing the terminal

# 1.1.5

**Adjustments:**

- Added HP Reverb as autodetected controller profile
- Added smooth rotation on the custom camera
- _Turn around..._

**Bug fixes:**

- Fixed local profile paths not working
- Fixed jitter in SteamVR

# 1.1.4

**Bug fixes:**

- Input not working (lol)
- Expanded performance options a bit
- Dynamic resolution filter is set to FSR by default

# 1.1.3

**Adjustments:**

- Added support for loading local files as controller binding overrides
- Added `index` as an official binding
- Added `wmr` as an official binding
- Fixed not being able to confirm builds
- Removed Herobrine

# 1.1.2

**Adjustments:**

- Removed the option `Dynamic Resolution`
- Enabled motion vector support, making DLSS look much better
- Added camera resolution scale option, defaulting to 0.75x normal (headset) resolution

# 1.1.1

**Adjustments:**

- Removed HDRP XR occlusion mesh shader

**Bug fixes:**

- Fixed charging items with secondary use not working
- Fixed not being able to cast a vote to leave early

# 1.1.0

**New Features:**

- Added basic finger tracking (By @Lakatrazz)
- Added room-scale crouching (By @Lakatrazz)
- Decoupled body from head rotation (By @Phil25)
- Made finger tracking thumb movement smoother
- Added thumbs up pose to finger tracking
- Added Virtual Keyboard in main menu
- Forced the game to be focused on startup
- Added additional logging if the game was not able to be started in VR
- Added support for building. With default bindings, hold both grip buttons to enter build mode and confirm builds. Press B to discard an item.

**Adjustments:**

- Change battery charge indicator position (By @abazilla)
- Increased shovel cooldown
- Shovel can no longer be used in certain situations
- Main menu/spectator/pause no longer have auto rotation, use the "Reset Height" button to move the screen in front of you
- Disabled lens distortion on the fear effect
- Adjusted input system, which can now autodetect the type of controller you are using. Still needs official bindings.
- Reverted a bunch of IK code which was causing issues. An actuall full IK fix will come in a future update.

**Bug fixes:**

- Made some changes to the Networking system, which should fix some desync issues
- Fixed interaction with pause menu whilst in spectator mode
- Fixed localization issue causing some users not to be able to launch the mod (shame on you Microsoft)
- Fixed jitter on scan nodes
- Fixed laser pointing not following hand movements for other players

**Added Config options:**

- Added snap turn degrees option
- Option to disable additional lens distortion effects to counter possible motion sickness

**Mod Compatibility:**

- Adjusted UI position on mimic fire exits (By @NickDuijndam)
- Added compatibility with TooManyEmotes without having to change configuration

## What's Changed

- Changed + fixed the README in some places by @StupidRepo in https://github.com/DaXcess/LCVR/pull/92
- Add config option to disable lens distortion by @DaXcess in https://github.com/DaXcess/LCVR/pull/125
- Move battery charge indicator + Added extra experiments for testing by @abazilla in https://github.com/DaXcess/LCVR/pull/104
- Decouple body from head rotation by @Phil25 in https://github.com/DaXcess/LCVR/pull/121
- Added finger tracking by @Lakatrazz in https://github.com/DaXcess/LCVR/pull/93
- Mimics compatibility: Offset mimic interact UI to be in the correct position by @NickDuijndam in https://github.com/DaXcess/LCVR/pull/138
- Fix shovel being able to be used in situations where it is not supposed to by @DaXcess in https://github.com/DaXcess/LCVR/pull/145
- Fix mod not working on Turkish (and probably others too) devices by @DaXcess in https://github.com/DaXcess/LCVR/pull/148
- Fix invisible pause menu by @DaXcess in https://github.com/DaXcess/LCVR/pull/149
- Add compatibility with TooManyEmotes by @DaXcess in https://github.com/DaXcess/LCVR/pull/146
- Automatic controller detection (framework) + Building support by @DaXcess in https://github.com/DaXcess/LCVR/pull/151
- Revert "Decouple body from head rotation" by @DaXcess in https://github.com/DaXcess/LCVR/pull/154
- config do support Enum `TurnProvider` by @louis1706 in https://github.com/DaXcess/LCVR/pull/152
- Revert "Revert "Decouple body from head rotation"" by @DaXcess in https://github.com/DaXcess/LCVR/pull/155
- FindIndex for Transpiler by @louis1706 in https://github.com/DaXcess/LCVR/pull/153
- Revert "FindIndex for Transpiler" by @DaXcess in https://github.com/DaXcess/LCVR/pull/157
- Better index for transpiler by @louis1706 in https://github.com/DaXcess/LCVR/pull/159
- Update main to 1.1.0 by @DaXcess in https://github.com/DaXcess/LCVR/pull/156

## New Contributors

- @StupidRepo made their first contribution in https://github.com/DaXcess/LCVR/pull/92
- @abazilla made their first contribution in https://github.com/DaXcess/LCVR/pull/104
- @Phil25 made their first contribution in https://github.com/DaXcess/LCVR/pull/121
- @Lakatrazz made their first contribution in https://github.com/DaXcess/LCVR/pull/93
- @NickDuijndam made their first contribution in https://github.com/DaXcess/LCVR/pull/138
- @louis1706 made their first contribution in https://github.com/DaXcess/LCVR/pull/152

**Full Changelog**: https://github.com/DaXcess/LCVR/compare/v1.0.1...v1.1.0

# 1.0.1

## What's Changed

- New discord invite link on startup by @J-Emil-P in https://github.com/DaXcess/LCVR/pull/88
- Revamped networking by @DaXcess in https://github.com/DaXcess/LCVR/pull/99

## New Contributors

- @J-Emil-P made their first contribution in https://github.com/DaXcess/LCVR/pull/88

**Full Changelog**: https://github.com/DaXcess/LCVR/compare/v1.0.0...v1.0.1

# 1.0.0

**The VR mod has finally released!!**

Finally, after almost exactly two months <sub><sup>_(started Nov 19th 2023)_</sup></sub> of hard work the Lethal Company VR mod has it's first release!

No changelogs are necessary for this version, as it is the first version. Subsequent versions will contain a list of changes and new contributors.

### Verifying mod signature

LCVR comes pre-packaged with a digital signature. You can use tools like GPG to verify the `LCVR.dll.sig` signature with the `LCVR.dll` plugin file.

The public key which can be used to verify the file is [9422426F6125277B82CC477DCF78CC72F0FD5EAD (OpenPGP Key Server)](https://keys.openpgp.org/vks/v1/by-fingerprint/9422426F6125277B82CC477DCF78CC72F0FD5EAD).
