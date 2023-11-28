# Lethal Company VR Mod

Collecting Scrap in VR

## Motion Controls

Motion controls are currently being worked on in a seperate plugin which does not have a repository.

Once enough advancements have been made on that front the code will be merged into this repo.

## TODO

- [ ] Complete controller support
- [ ] Get the correct FOV based on the headset being used. It looks like Unity somehow knows this already, just gotta figure out how to extract it.
- [ ] Build a working XRRig (or feasible alternative), that correctly applies player rotation based on head rotation
- [ ] Maybe hand movement based on controller movement? Would be cool for flashlights and stuff. Will have to check how item pickup/interactions will work though.
- [ ] Fix the item pickup/interact raycasting (currently still requires mouse input, even though it looks like nothing is happening)
- [ ] Fix non-player cameras (spectating, ship leaving after death, etc)
- [ ] Correctly display HUD by moving them to World Position and updating it's position/rotation based on HMD
- [ ] Check if we can prepare for a possible Il2Cpp version of the game. Currently the method patching transpilers will not work if the game were to be converted to Il2Cpp.
- [ ] (Optional) Controller haptics (e.g. vibrate when a Giant takes a step, when low health, when damaged, when in a panicked state etc).
- [ ] Be able to interact with the terminal, either via Virtual Keyboard (must be custom made as this mod avoids explicitly using SteamVR in favor of complete OpenXR support) or some other custom UI that has some presets.

- [ ] Probably a whole lot more, expect this list to be updated frequently

## Helpful tools and documentation

- [Unity Explorer](https://github.com/sinai-dev/UnityExplorer)
- [Unity OpenXR Plugin](https://docs.unity3d.com/Packages/com.unity.xr.openxr@1.8/manual/index.html)
- [Unity XR Inputs](https://docs.unity3d.com/Manual/xr_input.html)
- [OpenXR 1.0 Specification](https://registry.khronos.org/OpenXR/specs/1.0/html/xrspec.html)
