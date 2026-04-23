# Shader compatibility with LCVR

> This document is specifically for mods that add anything that can be rendered by the game, it is not about code or hooking into the VR mod.

With the VR mod *finally* hopping over to [Single Pass Instanced rendering](https://docs.unity3d.com/6000.6/Documentation/Manual/SinglePassInstancing.html), a lot of mods will break due to them missing SPI support in their exported shaders.

Single Pass Instanced rendering requires two things from shaders for them to render properly, which are:

- The shaders **must** support Stereo instancing (`UNITY_VERTEX_INPUT_INSTANCE_ID`, `UNITY_VERTEX_OUTPUT_STEREO`, etc)
- The shaders **must** be built with Stereo variants (OpenXR package needs to be installed)

# Supporting SPI in custom shaders

> Full technical details are explained [over here](https://docs.unity3d.com/6000.6/Documentation/Manual/SinglePassInstancing.html)

If you are shipping custom/hand written shaders in your mods, you must make changes to the shaders for them to work correctly.

These changes differ depending on what kind of shader you are writing (e.g. post processing shaders require additional changes).

If you are **not** using custom shaders, you do not have to modify anything.

# Exporting shaders with Stereo variants

Even if your shaders already support SPI rendering (whether you're only using HDRP built-in shaders, or your custom shaders already support SPI), Unity will still strip this support out of the shaders by default.

To tell Unity to also build in SPI support when exporting your assets, you will have to install the Unity OpenXR plugin.

<img width="1279" height="351" alt="image" src="https://github.com/user-attachments/assets/fe8bbcee-19ce-4ff0-a190-016e0cd3dfc9" />

> You can also add the package by name: `com.unity.xr.openxr`

You do not have to configure anything after installing this plugin, just re-export your asset bundle and all bundled shaders will now have SPI variants built in.

> In the case play mode starts behaving weirdly, disable VR by going to `Player Settings` -> `XR Plug-in Management` and disabling the `Initialize XR on Startup` option

# Note about Lethal Level Loader

Whenever you re-export your assets in a Lethal Level Loader mod, make sure both the scene lethalbundle file and the assets lethalbundle file have been updated. I've seen multiple reports of modded moons only *partially* working and it has almost always come down to the scene bundle supporting OpenXR rendering, but the assets bundle was left untouched, meaning all shaders and materials in the other bundle failed to render properly.
