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
