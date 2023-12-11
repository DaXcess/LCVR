# ⚠️⚠️ NOT READY YET ⚠️⚠️
> The existence of this GitHub repository does not mean a working mod already exists, it's just non-functional source code at this time

# Lethal Company VR Mod

Collecting Scrap in VR

## Performance

Since Lethal Company is using the High Definition Render Pipeline, enabling XR causes a lot of performance issues. Luckily we can use Dynamic Resolution to alleviate some of the lag.

At the time of writing, there has been a very simple initial draft added to the code that enables Dynamic Resolution with a Catmull Rom upscale filter, with an aggressive setting of 25%-50% screen scale. More research and testing needs to be done to see if it's also feasible to use DLSS/FSR, without compromising quality and with adequate performance gains.

## Helpful tools and documentation

- [Unity Explorer](https://github.com/sinai-dev/UnityExplorer)
- [Unity OpenXR Plugin](https://docs.unity3d.com/Packages/com.unity.xr.openxr@1.8/manual/index.html)
- [Unity XR Inputs](https://docs.unity3d.com/Manual/xr_input.html)
